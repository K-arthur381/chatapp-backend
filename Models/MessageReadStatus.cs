namespace ChatApp.Api.Models
{
    public class MessageReadStatus
    {
        public Guid ReadStatusId { get; set; } = Guid.NewGuid();
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
