namespace ChatApp.Api.Models
{
    public class Participant
    {
        public Guid ParticipantId { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Member"; // "Admin" or "Member"
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
