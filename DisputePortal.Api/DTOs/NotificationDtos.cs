namespace DisputePortal.Api.DTOs;

public record NotificationResponse(
    int Id,
    string Subject,
    string Message,
    DateTime SentAt
);
