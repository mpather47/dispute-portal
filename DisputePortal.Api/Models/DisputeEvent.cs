namespace DisputePortal.Api.Models
{
    public class DisputeEvent
    {
        public int Id { get; set; }
        public DisputeStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int DisputeId { get; set; }
        public Dispute Dispute { get; set; } = null!;
    }
}
