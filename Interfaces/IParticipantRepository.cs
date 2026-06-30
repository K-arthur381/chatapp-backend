using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IParticipantRepository : IGenericRepository<Participant>
    {
        Task<bool> IsUserInConversationAsync(Guid userId, Guid conversationId);
        Task<IReadOnlyList<Participant>> GetParticipantsByConversationAsync(Guid conversationId);
        Task<List<Participant>> GetParticipantsListAsync(Guid conversationId);
        Task<Participant?> GetByUserAndConversationAsync(Guid userId, Guid conversationId);
    }
}
