namespace DePinCore.Application.DTOs;

public class TelemetryDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public long NetworkIn { get; set; }
    public long NetworkOut { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class NodeDto
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public DateTime? LastTelemetryReceived { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsActive { get; set; }
    public int ConsecutiveUnhealthyChecks { get; set; }
}
