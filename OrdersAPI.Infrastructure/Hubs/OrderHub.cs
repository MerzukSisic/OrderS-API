using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrdersAPI.Infrastructure.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Get user role from JWT token
        var userRole = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        
        if (!string.IsNullOrEmpty(userRole))
        {
            // Add user to their role group (Admin, Kitchen, Bartender, Waiter)
            await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
            Console.WriteLine($"✅ SignalR: User connected to {userRole} group - ConnectionId: {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userRole = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        
        if (!string.IsNullOrEmpty(userRole))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRole);
            Console.WriteLine($"❌ SignalR: User disconnected from {userRole} group - ConnectionId: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client can manually join specific groups if needed
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftGroup", groupName);
    }
}