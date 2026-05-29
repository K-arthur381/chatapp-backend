namespace ChatApp.Api.Models
{
    public class Conversation
    {
        public Guid ConversationId { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = "Private"; // "Private" or "Group"
        public string? GroupName { get; set; }
        public string? GroupAvatarUrl { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public User Creator { get; set; } = null!;
        public ICollection<Participant> Participants { get; set; } = new List<Participant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
