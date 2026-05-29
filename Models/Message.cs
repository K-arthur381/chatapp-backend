namespace ChatApp.Api.Models
{
    public class Message
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string? Content { get; set; }
        public string MessageType { get; set; } = "Text"; // Text, Image, File
        public Guid? ReplyToMessageId { get; set; }
        public Guid? ForwardedFromUserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }  
        public bool IsEdited => EditedAt.HasValue;
        public bool IsDeleted { get; set; } = false;
        public bool IsSystemMessage { get; set; } = false;

        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
        public Message? ReplyToMessage { get; set; }
        public User? ForwardedFromUser { get; set; }
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageReadStatus> ReadStatuses { get; set; } = new List<MessageReadStatus>();
    }
}
