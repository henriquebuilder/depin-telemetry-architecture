using DePinCore.Domain.Entities;

namespace DePinCore.Application.Interfaces;

public interface INodeRepository
{
    Task<Node?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<Node?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Node>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Node>> GetUnhealthyNodesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Node node, CancellationToken cancellationToken = default);
    Task UpdateAsync(Node node, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken = default);
}
