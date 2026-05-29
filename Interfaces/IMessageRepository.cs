using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<Message> GetByMessageIdAsync(Guid messageId);
        Task<IReadOnlyList<Message>> GetMessagesByConversationAsync(Guid conversationId, int skip, int take);
        Task<Message?> GetMessageWithDetailsAsync(Guid messageId);
    }
}
