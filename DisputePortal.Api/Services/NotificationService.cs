using DisputePortal.Api.Data;
using DisputePortal.Api.Models;

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
}
