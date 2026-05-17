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
    List<DisputeEventResponse> Events
);

public record DisputeEventResponse(
    string Status,
    string Message,
    string CreatedBy,
    DateTime CreatedAt
);
