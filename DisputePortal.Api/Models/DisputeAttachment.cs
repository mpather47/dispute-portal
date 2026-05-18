namespace DisputePortal.Api.Models;

public class DisputeAttachment
{
    public int Id { get; set; }
    public int DisputeId { get; set; }
    public Dispute Dispute { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }

    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
