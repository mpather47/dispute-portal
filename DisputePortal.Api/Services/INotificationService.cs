namespace DisputePortal.Api.Services;

public interface INotificationService
{
    Task LogNotificationAsync(string recipient, string subject, string message);
}
