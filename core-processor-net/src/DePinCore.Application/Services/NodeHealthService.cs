using DePinCore.Application.DTOs;
using DePinCore.Application.Interfaces;
using DePinCore.Domain.Entities;
using DePinCore.Domain.Services;

namespace DePinCore.Application.Services;

public class NodeHealthService : INodeHealthService
{
    private readonly INodeRepository _nodeRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly NodeHealthValidator _healthValidator;

    public NodeHealthService(
        INodeRepository nodeRepository,
        ITelemetryRepository telemetryRepository,
        NodeHealthValidator healthValidator)
    {
        _nodeRepository = nodeRepository;
        _telemetryRepository = telemetryRepository;
        _healthValidator = healthValidator;
    }

    public async Task<NodeHealthStatus> ProcessTelemetryAsync(NodeTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        var node = await GetOrCreateNodeAsync(
            telemetry.DeviceId,
            telemetry.DeviceType,
            telemetry.Location,
            cancellationToken);

        var healthStatus = _healthValidator.ValidateHealth(telemetry);
        telemetry.UpdateHealthStatus(healthStatus);

        node.UpdateTelemetry(telemetry);
        node.UpdateHealthStatus(healthStatus);

        await _telemetryRepository.AddAsync(telemetry, cancellationToken);
        await _nodeRepository.UpdateAsync(node, cancellationToken);

        return healthStatus;
    }

    public async Task<Node> GetOrCreateNodeAsync(string deviceId, string deviceType, string location, CancellationToken cancellationToken = default)
    {
        var existingNode = await _nodeRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
        
        if (existingNode != null)
            return existingNode;

        var newNode = new Node(deviceId, deviceType, location);
        await _nodeRepository.AddAsync(newNode, cancellationToken);
        return newNode;
    }

    public async Task<IEnumerable<Node>> GetUnhealthyNodesAsync(CancellationToken cancellationToken = default)
    {
        return await _nodeRepository.GetUnhealthyNodesAsync(cancellationToken);
    }

    public async Task<bool> ShouldAlertAsync(Node node, CancellationToken cancellationToken = default)
    {
        return _healthValidator.ShouldTriggerAlert(node);
    }
}
