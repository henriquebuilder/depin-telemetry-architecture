using DePinCore.Domain.Entities;

namespace DePinCore.Application.Interfaces;

public interface INodeHealthService
{
    Task<NodeHealthStatus> ProcessTelemetryAsync(NodeTelemetry telemetry, CancellationToken cancellationToken = default);
    Task<Node> GetOrCreateNodeAsync(string deviceId, string deviceType, string location, CancellationToken cancellationToken = default);
    Task<IEnumerable<Node>> GetUnhealthyNodesAsync(CancellationToken cancellationToken = default);
    Task<bool> ShouldAlertAsync(Node node, CancellationToken cancellationToken = default);
}
