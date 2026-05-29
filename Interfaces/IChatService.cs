using ChatApp.Api.DTOs;

namespace ChatApp.Api.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<Guid>> GetUserConversationIds(Guid userId);
        Task<MessageDto> SaveMessage(Guid senderId, SendMessageDto dto);
        Task<MessageDto> MarkMessageAsRead(Guid messageId, Guid userId);
        Task<ReactionDto> ToggleReaction(Guid messageId, Guid userId, string reaction);
        Task<IEnumerable<MessageDto>> LoadMessages(Guid conversationId, int skip, int take);
        Task<MessageDto?> GetMessageById(Guid messageId);
    }
}
