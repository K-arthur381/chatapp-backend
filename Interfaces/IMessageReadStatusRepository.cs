using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IMessageReadStatusRepository : IGenericRepository<MessageReadStatus>
    {
        Task<MessageReadStatus?> GetByMessageAndUserAsync(Guid messageId, Guid userId);
        Task<IList<MessageReadStatus>> GetReadStatusesForMessageAsync(Guid messageId);
        Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
        Task MarkAllAsReadAsync(Guid conversationId, Guid userId);
    }
}
