using System.ComponentModel.DataAnnotations;
using DisputePortal.Api.Models;

namespace DisputePortal.Api.DTOs;

public record CreateDisputeRequest(
    [Range(1, int.MaxValue, ErrorMessage = "A valid transaction ID is required.")] int TransactionId,
    [Required, MaxLength(500)] string Reason,
    [MaxLength(2000)] string CustomerNotes
);

public record UpdateDisputeStatusRequest(
    [Required] DisputeStatus Status,
    [MaxLength(2000)] string? AdminNotes
);

public record ReplyRequest(
    [Required, MaxLength(2000)] string Message
);

public record AttachmentResponse(
    int Id,
    string FileName,
    string ContentType,
    long FileSize,
    string UploadedBy,
    DateTime UploadedAt
);

public record DisputeResponse(
    int Id,
    string CaseNumber,
    int TransactionId,
    string MerchantName,
    decimal Amount,
    string Reason,
    string CustomerNotes,
    string? AdminNotes,
    string Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    List<DisputeEventResponse> Events,
    List<AttachmentResponse> Attachments,
    string? CustomerName = null
);

public record DisputeEventResponse(
    string Status,
    string Message,
    string CreatedBy,
    DateTime CreatedAt
);

public record AdminStatsResponse(
    int Total,
    int OpenCount,
    int SubmittedToday,
    double AvgResolutionDays,
    Dictionary<string, int> ByStatus
);
