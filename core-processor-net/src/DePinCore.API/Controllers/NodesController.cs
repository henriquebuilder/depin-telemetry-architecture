using DePinCore.Application.Interfaces;
using DePinCore.Application.DTOs;
using DePinCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DePinCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INodeHealthService _nodeHealthService;
    private readonly IHubContext<NodeHealthHub> _hubContext;

    public NodesController(
        INodeHealthService nodeHealthService,
        IHubContext<NodeHealthHub> hubContext)
    {
        _nodeHealthService = nodeHealthService;
        _hubContext = hubContext;
    }

    [HttpGet("unhealthy")]
    public async Task<ActionResult<IEnumerable<NodeDto>>> GetUnhealthyNodes(CancellationToken cancellationToken = default)
    {
        var unhealthyNodes = await _nodeHealthService.GetUnhealthyNodesAsync(cancellationToken);
        
        var nodeDtos = unhealthyNodes.Select(node => new NodeDto
        {
            Id = node.Id,
            DeviceId = node.DeviceId,
            DeviceType = node.DeviceType,
            Location = node.Location,
            HealthStatus = node.CurrentHealthStatus.ToString(),
            LastTelemetryReceived = node.LastTelemetryReceived,
            RegisteredAt = node.RegisteredAt,
            IsActive = node.IsActive,
            ConsecutiveUnhealthyChecks = node.ConsecutiveUnhealthyChecks
        });

        return Ok(nodeDtos);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NodeDto>>> GetAllNodes(CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeHealthService.GetUnhealthyNodesAsync(cancellationToken);
        
        var nodeDtos = nodes.Select(node => new NodeDto
        {
            Id = node.Id,
            DeviceId = node.DeviceId,
            DeviceType = node.DeviceType,
            Location = node.Location,
            HealthStatus = node.CurrentHealthStatus.ToString(),
            LastTelemetryReceived = node.LastTelemetryReceived,
            RegisteredAt = node.RegisteredAt,
            IsActive = node.IsActive,
            ConsecutiveUnhealthyChecks = node.ConsecutiveUnhealthyChecks
        });

        return Ok(nodeDtos);
    }
}
