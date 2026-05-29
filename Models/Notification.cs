namespace ChatApp.Api.Models
{
    public class Notification
    {
        public Guid NotificationId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty; // NewMessage, Reaction, System
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? ReferenceId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
