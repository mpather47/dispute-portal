namespace DisputePortal.Api.Models;

public class Dispute
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CustomerNotes { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Submitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public int TransactionId { get; set; }
    public BankTransaction Transaction { get; set; } = null!;

    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser Customer { get; set; } = null!;

    public List<DisputeEvent> Events { get; set; } = new();
}