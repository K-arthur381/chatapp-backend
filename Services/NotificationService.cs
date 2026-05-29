using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;

public class NotificationService : INotificationService
{
    // Placeholder for push notifications (FCM, etc.)
    public async Task SendMessageNotification(Message message)
    {
        // In production: lookup recipient device tokens, fire and forget
        await Task.CompletedTask;
    }
}