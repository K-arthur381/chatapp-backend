using ChatApp.Api.DTOs;
using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface INotificationService
    {
        Task SendMessageNotificationAsync(Message message, Guid senderId);
        Task DeliverPendingNotificationsAsync(Guid userId);
        Task SendReactionNotificationAsync(Guid messageId, Guid reactorId, string reaction);
        Task SendGroupInviteNotificationAsync(Guid conversationId, Guid adminId, Guid invitedUserId, string groupName);
        Task SendKickedNotificationAsync(Guid conversationId, Guid adminId, Guid kickedUserId, string groupName);
        Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, int skip, int take);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}
