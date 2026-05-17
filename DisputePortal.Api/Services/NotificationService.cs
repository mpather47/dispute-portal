using DisputePortal.Api.Data;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Hubs;
using DisputePortal.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DisputePortal.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task LogNotificationAsync(
        string recipient,
        string subject,
        string message)
    {
        var log = new NotificationLog
        {
            Recipient = recipient,
            Subject = subject,
            Message = message,
            SentAt = DateTime.UtcNow
        };

        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(recipient).SendAsync(
            "ReceiveNotification",
            new NotificationResponse(log.Id, log.Subject, log.Message, log.SentAt)
        );
    }

    public async Task<List<NotificationResponse>> GetForRecipientAsync(string email)
    {
        return await _db.NotificationLogs
            .Where(n => n.Recipient == email)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationResponse(n.Id, n.Subject, n.Message, n.SentAt))
            .ToListAsync();
    }
}
