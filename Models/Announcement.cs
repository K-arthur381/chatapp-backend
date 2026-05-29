namespace ChatApp.Api.Models
{
    public class Announcement
    {
        public Guid AnnouncementId { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public User Creator { get; set; } = null!;
    }
}
