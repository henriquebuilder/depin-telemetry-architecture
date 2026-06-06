namespace DePinCore.Domain.Entities;

public class NodeTelemetry
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; }
    public string DeviceType { get; private set; }
    public string Location { get; private set; }
    public DateTime Timestamp { get; private set; }
    public double CpuUsage { get; private set; }
    public double MemoryUsage { get; private set; }
    public double DiskUsage { get; private set; }
    public long NetworkIn { get; private set; }
    public long NetworkOut { get; private set; }
    public Dictionary<string, object> Metrics { get; private set; }
    public NodeHealthStatus HealthStatus { get; private set; }
    public DateTime? LastHealthCheck { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NodeTelemetry() { }

    public NodeTelemetry(
        string deviceId,
        string deviceType,
        string location,
        double cpuUsage,
        double memoryUsage,
        double diskUsage,
        long networkIn,
        long networkOut,
        Dictionary<string, object>? metrics = null)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        DeviceType = deviceType;
        Location = location;
        Timestamp = DateTime.UtcNow;
        CpuUsage = cpuUsage;
        MemoryUsage = memoryUsage;
        DiskUsage = diskUsage;
        NetworkIn = networkIn;
        NetworkOut = networkOut;
        Metrics = metrics ?? new Dictionary<string, object>();
        HealthStatus = NodeHealthStatus.Unknown;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateHealthStatus(NodeHealthStatus status)
    {
        HealthStatus = status;
        LastHealthCheck = DateTime.UtcNow;
    }

    public bool IsHealthy()
    {
        return HealthStatus == NodeHealthStatus.Healthy;
    }

    public bool IsUnhealthy()
    {
        return HealthStatus == NodeHealthStatus.Unhealthy;
    }

    public bool IsDegraded()
    {
        return HealthStatus == NodeHealthStatus.Degraded;
    }
}

public enum NodeHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy
}
