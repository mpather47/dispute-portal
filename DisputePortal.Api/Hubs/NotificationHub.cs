using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DisputePortal.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Email);
        if (email is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, email);

        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

        await base.OnConnectedAsync();
    }
}
