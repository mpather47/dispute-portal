using DisputePortal.Api.Models;

namespace DisputePortal.Api.DTOs;

public record CreateDisputeRequest(
    int TransactionId,
    string Reason,
    string CustomerNotes
);

public record UpdateDisputeStatusRequest(
    DisputeStatus Status,
    string? AdminNotes
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