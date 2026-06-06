using DePinCore.Domain.Entities;

namespace DePinCore.Application.Interfaces;

public interface ITelemetryRepository
{
    Task AddAsync(NodeTelemetry telemetry, CancellationToken cancellationToken = default);
    Task<NodeTelemetry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodeTelemetry>> GetByDeviceIdAsync(string deviceId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<NodeTelemetry>> GetRecentAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default);
}
