namespace DePinCore.Domain.Entities;

public class Node
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; }
    public string DeviceType { get; private set; }
    public string Location { get; private set; }
    public NodeHealthStatus CurrentHealthStatus { get; private set; }
    public DateTime? LastTelemetryReceived { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public bool IsActive { get; private set; }
    public int ConsecutiveUnhealthyChecks { get; private set; }
    public ICollection<NodeTelemetry> TelemetryHistory { get; private set; }

    private Node() { }

    public Node(string deviceId, string deviceType, string location)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        DeviceType = deviceType;
        Location = location;
        CurrentHealthStatus = NodeHealthStatus.Unknown;
        RegisteredAt = DateTime.UtcNow;
        IsActive = true;
        ConsecutiveUnhealthyChecks = 0;
        TelemetryHistory = new List<NodeTelemetry>();
    }

    public void UpdateTelemetry(NodeTelemetry telemetry)
    {
        telemetry.UpdateHealthStatus(CurrentHealthStatus);
        TelemetryHistory.Add(telemetry);
        LastTelemetryReceived = DateTime.UtcNow;
    }

    public void UpdateHealthStatus(NodeHealthStatus status)
    {
        CurrentHealthStatus = status;
        
        if (status == NodeHealthStatus.Unhealthy)
        {
            ConsecutiveUnhealthyChecks++;
        }
        else
        {
            ConsecutiveUnhealthyChecks = 0;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public bool ShouldAlert()
    {
        return ConsecutiveUnhealthyChecks >= 3;
    }

    public TimeSpan GetTimeSinceLastTelemetry()
    {
        if (!LastTelemetryReceived.HasValue)
            return TimeSpan.FromDays(365);
        
        return DateTime.UtcNow - LastTelemetryReceived.Value;
    }
}
