namespace ChatApp.Api.Models
{
    public class PendingNotification
    {
        public Guid PendingId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDelivered { get; set; } = false;
        public User User { get; set; } = null!;
    }
}
