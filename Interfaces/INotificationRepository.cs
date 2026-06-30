using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(Guid userId, int skip, int take);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}
