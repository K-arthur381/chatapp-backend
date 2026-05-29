using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId);
        Task<IReadOnlyList<Guid>> GetParticipantIdsAsync(Guid conversationId);
    }
}
