using DePinCore.Application.Interfaces;
using DePinCore.Domain.Entities;
using DePinCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DePinCore.Infrastructure.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly AppDbContext _context;

    public NodeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Node?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Nodes
            .Include(n => n.TelemetryHistory.OrderByDescending(t => t.Timestamp).Take(10))
            .FirstOrDefaultAsync(n => n.DeviceId == deviceId, cancellationToken);
    }

    public async Task<Node?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Nodes
            .Include(n => n.TelemetryHistory.OrderByDescending(t => t.Timestamp).Take(10))
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Node>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Nodes
            .Include(n => n.TelemetryHistory.OrderByDescending(t => t.Timestamp).Take(10))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Node>> GetUnhealthyNodesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Nodes
            .Where(n => n.CurrentHealthStatus == Domain.Entities.NodeHealthStatus.Unhealthy || 
                       n.ConsecutiveUnhealthyChecks >= 3)
            .Include(n => n.TelemetryHistory.OrderByDescending(t => t.Timestamp).Take(10))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Node node, CancellationToken cancellationToken = default)
    {
        await _context.Nodes.AddAsync(node, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Node node, CancellationToken cancellationToken = default)
    {
        _context.Nodes.Update(node);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Nodes.AnyAsync(n => n.DeviceId == deviceId, cancellationToken);
    }
}
