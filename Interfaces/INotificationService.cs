using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface INotificationService
    {
        Task SendMessageNotification(Message message);
    }
}
