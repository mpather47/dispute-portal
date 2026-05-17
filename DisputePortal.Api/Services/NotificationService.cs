using DisputePortal.Api.Data;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DisputePortal.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogNotificationAsync(
        string recipient,
        string subject,
        string message)
    {
        _db.NotificationLogs.Add(new NotificationLog
        {
            Recipient = recipient,
            Subject = subject,
            Message = message,
            SentAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
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
