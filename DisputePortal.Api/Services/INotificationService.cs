using DisputePortal.Api.DTOs;

namespace DisputePortal.Api.Services;

public interface INotificationService
{
    Task LogNotificationAsync(string recipient, string subject, string message);
    Task<List<NotificationResponse>> GetForRecipientAsync(string email);
    Task PushDisputeEventAsync(string group, string eventName);
}
