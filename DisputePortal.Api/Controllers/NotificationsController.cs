using System.Security.Claims;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetMyNotifications()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("Email claim is missing.");

        var notifications = await _notificationService.GetForRecipientAsync(email);
        return Ok(notifications);
    }
}
