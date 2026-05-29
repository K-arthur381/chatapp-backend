namespace ChatApp.Api.Models
{
    public class Attachment
    {
        public Guid AttachmentId { get; set; } = Guid.NewGuid();
        public Guid MessageId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? DurationSeconds { get; set; }

        public Message Message { get; set; } = null!;
    }
}
