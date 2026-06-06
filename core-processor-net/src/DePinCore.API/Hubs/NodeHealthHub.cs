using Microsoft.AspNetCore.SignalR;

namespace DePinCore.API.Hubs;

public class NodeHealthHub : Hub
{
    public async Task JoinNodeGroup(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"node_{deviceId}");
        await Clients.Caller.SendAsync("JoinedGroup", $"Joined monitoring group for node {deviceId}");
    }

    public async Task LeaveNodeGroup(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"node_{deviceId}");
        await Clients.Caller.SendAsync("LeftGroup", $"Left monitoring group for node {deviceId}");
    }
}
