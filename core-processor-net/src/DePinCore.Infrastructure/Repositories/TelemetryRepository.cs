using DePinCore.Application.Interfaces;
using DePinCore.Domain.Entities;
using DePinCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DePinCore.Infrastructure.Repositories;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly AppDbContext _context;

    public TelemetryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NodeTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        await _context.NodeTelemetries.AddAsync(telemetry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<NodeTelemetry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NodeTelemetries
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<NodeTelemetry>> GetByDeviceIdAsync(string deviceId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.NodeTelemetries
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NodeTelemetry>> GetRecentAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return await _context.NodeTelemetries
            .Where(t => t.Timestamp >= cutoff)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
