namespace ChatApp.Api.Models
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; }  = DateTime.UtcNow;
        public bool IsOnline { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageReadStatus> ReadStatuses { get; set; } = new List<MessageReadStatus>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
