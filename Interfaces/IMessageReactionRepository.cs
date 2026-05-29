using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IMessageReactionRepository : IGenericRepository<MessageReaction>
    {
        Task<MessageReaction?> GetByMessageAndUserAsync(Guid messageId, Guid userId);
        Task<IList<MessageReaction>> GetReactionsForMessageAsync(Guid messageId);
    }
}
