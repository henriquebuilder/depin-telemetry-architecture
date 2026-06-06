using System.Text;
using System.Text.Json;
using DePinCore.Application.Interfaces;
using DePinCore.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DePinCore.Infrastructure.BackgroundServices;

public class TelemetryConsumerService : BackgroundService
{
    private readonly INodeHealthService _nodeHealthService;
    private readonly ILogger<TelemetryConsumerService> _logger;
    private readonly IHubContext<NodeHealthHub> _hubContext;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _rabbitMQUrl;
    private readonly string _queueName = "telemetry_queue";
    private readonly string _exchangeName = "telemetry_exchange";

    public TelemetryConsumerService(
        INodeHealthService nodeHealthService,
        ILogger<TelemetryConsumerService> logger,
        IHubContext<NodeHealthHub> hubContext,
        IConfiguration configuration)
    {
        _nodeHealthService = nodeHealthService;
        _logger = logger;
        _hubContext = hubContext;
        _rabbitMQUrl = configuration.GetValue<string>("RabbitMQ:Url") ?? "amqp://guest:guest@localhost:5672/";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telemetry Consumer Service starting");

        try
        {
            await InitializeRabbitMQAsync(stoppingToken);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var telemetry = JsonSerializer.Deserialize<NodeTelemetry>(message);

                    if (telemetry != null)
                    {
                        _logger.LogInformation("Processing telemetry for device {DeviceId}", telemetry.DeviceId);
                        
                        var healthStatus = await _nodeHealthService.ProcessTelemetryAsync(telemetry, stoppingToken);
                        
                        _logger.LogInformation("Device {DeviceId} health status: {HealthStatus}", 
                            telemetry.DeviceId, healthStatus);

                        if (healthStatus == NodeHealthStatus.Unhealthy)
                        {
                            _logger.LogWarning("Device {DeviceId} is UNHEALTHY - Alert triggered", telemetry.DeviceId);
                            await TriggerAlertAsync(telemetry, stoppingToken);
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(_queueName, false, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telemetry Consumer Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetry Consumer Service failed");
            throw;
        }
    }

    private async Task InitializeRabbitMQAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = _rabbitMQUrl };
        
        for (int i = 0; i < 5; i++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken);
                
                await _channel.ExchangeDeclareAsync(
                    _exchangeName,
                    ExchangeType.Topic,
                    true,
                    false,
                    cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    _queueName,
                    true,
                    false,
                    false,
                    cancellationToken: cancellationToken);

                await _channel.QueueBindAsync(
                    _queueName,
                    _exchangeName,
                    "telemetry.#",
                    cancellationToken: cancellationToken);

                await _channel.BasicQosAsync(0, 10, false, cancellationToken);
                
                _logger.LogInformation("RabbitMQ connection established");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/5)", i + 1);
                await Task.Delay(5000, cancellationToken);
            }
        }

        throw new InvalidOperationException("Failed to connect to RabbitMQ after 5 attempts");
    }

    private async Task TriggerAlertAsync(NodeTelemetry telemetry, CancellationToken cancellationToken)
    {
        _logger.LogWarning("ALERT: Node {DeviceId} is unhealthy - CPU: {CPU}%, Memory: {Memory}%, Disk: {Disk}%",
            telemetry.DeviceId, telemetry.CpuUsage, telemetry.MemoryUsage, telemetry.DiskUsage);

        var alertData = new
        {
            DeviceId = telemetry.DeviceId,
            DeviceType = telemetry.DeviceType,
            Location = telemetry.Location,
            HealthStatus = "Unhealthy",
            CpuUsage = telemetry.CpuUsage,
            MemoryUsage = telemetry.MemoryUsage,
            DiskUsage = telemetry.DiskUsage,
            Timestamp = telemetry.Timestamp
        };

        await _hubContext.Clients.Group($"node_{telemetry.DeviceId}").SendAsync("NodeHealthAlert", alertData, cancellationToken);
        await _hubContext.Clients.All.SendAsync("UnhealthyNodeDetected", alertData, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telemetry Consumer Service stopping");
        
        _channel?.Close();
        _connection?.Close();
        
        await base.StopAsync(cancellationToken);
    }
}
