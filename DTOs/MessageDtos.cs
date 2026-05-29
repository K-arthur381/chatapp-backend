namespace ChatApp.Api.DTOs
{

    public record SendMessageDto(
     Guid ConversationId,
     string? Content,
     string MessageType,
     Guid? ReplyToMessageId = null,
     Guid? ForwardFromMessageId = null,
     string? FileName = null,
     long FileSize = 0 ,
     List<AttachmentData>? Attachments = null
 );

    public record AttachmentData(
    string FileUrl,
    string? FileName,
    string ContentType,
    long FileSize
);

    public record MessageDto(
        Guid MessageId,
        Guid ConversationId,
        UserDto Sender,
        string? Content,
        string MessageType,
        DateTime SentAt,
        Guid? ReplyToMessageId,
        ReplyInfoDto? ReplyToMessage,
        UserDto? ForwardedFrom,
        IEnumerable<AttachmentDto> Attachments,
        Dictionary<string, int> ReactionCounts,
        string? CurrentUserReaction,
        int ReadCount,
        int TotalParticipants
    );

    public record AttachmentDto(string FileUrl, string? FileName, string ContentType, long FileSize, string? ThumbnailUrl, int? DurationSeconds);

    public record ReplyInfoDto(Guid MessageId, string? Content, string SenderName);

    public record ReactionDto(Guid MessageId, Guid ConversationId, Guid UserId, string UserName, string Reaction);
}
