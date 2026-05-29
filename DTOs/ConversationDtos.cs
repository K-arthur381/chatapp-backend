namespace ChatApp.Api.DTOs
{

    public record CreateConversationDto(
        string Type,
        string? GroupName,
        List<Guid> MemberIds,
        string? GroupAvatarUrl = null
        );

    public record ConversationDto(
        Guid ConversationId, 
        string Type,
        string? GroupName,
        string? GroupAvatarUrl,
        List<UserDto> Participants, 
        MessageDto? LastMessage,
        int UnreadCount = 0
        );
}
