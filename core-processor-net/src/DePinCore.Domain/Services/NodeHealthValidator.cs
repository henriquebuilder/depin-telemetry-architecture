namespace DePinCore.Domain.Services;

public class NodeHealthValidator
{
    private const double CpuThreshold = 80.0;
    private const double MemoryThreshold = 85.0;
    private const double DiskThreshold = 90.0;
    private const TimeSpan TelemetryTimeout = TimeSpan.FromMinutes(5);

    public NodeHealthStatus ValidateHealth(NodeTelemetry telemetry)
    {
        var issues = new List<string>();

        if (telemetry.CpuUsage > CpuThreshold)
            issues.Add($"CPU usage ({telemetry.CpuUsage:F2}%) exceeds threshold ({CpuThreshold}%)");

        if (telemetry.MemoryUsage > MemoryThreshold)
            issues.Add($"Memory usage ({telemetry.MemoryUsage:F2}%) exceeds threshold ({MemoryThreshold}%)");

        if (telemetry.DiskUsage > DiskThreshold)
            issues.Add($"Disk usage ({telemetry.DiskUsage:F2}%) exceeds threshold ({DiskThreshold}%)");

        if (issues.Count == 0)
            return NodeHealthStatus.Healthy;

        if (issues.Count == 1)
            return NodeHealthStatus.Degraded;

        return NodeHealthStatus.Unhealthy;
    }

    public NodeHealthStatus ValidateNodeHealth(Node node)
    {
        if (!node.IsActive)
            return NodeHealthStatus.Unhealthy;

        var timeSinceLastTelemetry = node.GetTimeSinceLastTelemetry();
        if (timeSinceLastTelemetry > TelemetryTimeout)
            return NodeHealthStatus.Unhealthy;

        return node.CurrentHealthStatus;
    }

    public bool ShouldTriggerAlert(Node node)
    {
        if (!node.IsActive)
            return true;

        if (node.ShouldAlert())
            return true;

        var timeSinceLastTelemetry = node.GetTimeSinceLastTelemetry();
        if (timeSinceLastTelemetry > TelemetryTimeout)
            return true;

        return false;
    }
}
