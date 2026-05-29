namespace ChatApp.Api.Models
{
    public class MessageReaction
    {
        public Guid ReactionId { get; set; } = Guid.NewGuid();
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string Reaction { get; set; } = string.Empty; // like, love, haha, wow, sad, angry
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
