using ChatApp.Api.Data;
using ChatApp.Api.DTOs;
using ChatApp.Api.Hubs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ConnectionManager _connectionManager;
    private readonly IFcmService _fcmService;        
    private readonly AppDbContext _context;           

    public NotificationService(
        IParticipantRepository participantRepository,
        INotificationRepository notificationRepo,
        IUserRepository userRepo,
        IHubContext<ChatHub> hubContext,
        ConnectionManager connectionManager,
        IFcmService fcmService,                     
        AppDbContext context)                        
    {
        _participantRepo = participantRepository;
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _fcmService = fcmService;
        _context = context;
    }

    public async Task SendMessageNotificationAsync(Message message, Guid senderId)
    {
        var sender = await _userRepo.GetByIdAsync(senderId);
        var participants = await GetConversationParticipants(message.ConversationId);

        foreach (var participantId in participants)
        {
            if (participantId == senderId) continue;

            var notification = new Notification
            {
                UserId = participantId,
                Type = "NewMessage",
                Title = $"New message from {sender?.Username}",
                Body = message.Content?.Length > 100 ? message.Content[..100] + "..." : message.Content,
                ReferenceId = message.ConversationId
            };
            await _notificationRepo.AddAsync(notification);

            // ✅ Check if user is online
            var isOnline = _connectionManager.IsUserOnline(participantId);
           // var isOnline = false;
            if (isOnline)
            {
                // ✅ Online → Send real-time via SignalR
                await SendRealTimeNotification(participantId, notification);
                Console.WriteLine($"📱 Real-time notification sent to ONLINE user: {participantId}");
            }
            else
            {
                // ✅ Offline → Store pending + Send FCM push
                await StorePendingNotification(participantId, notification);
                await SendPushToUserDevices(
                    participantId,
                    notification.Title,
                    notification.Body ?? "",
                    message.ConversationId.ToString()
                );
                Console.WriteLine($"📲 FCM push sent to OFFLINE user: {participantId}");
            }
        }
        await _notificationRepo.SaveChangesAsync();
    }

    public async Task SendReactionNotificationAsync(Guid messageId, Guid reactorId, string reaction)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return;

        var reactor = await _userRepo.GetByIdAsync(reactorId);
        var notification = new Notification
        {
            UserId = message.SenderId,
            Type = "Reaction",
            Title = $"{reactor?.Username} reacted {reaction}",
            Body = "to your message",
            ReferenceId = message.ConversationId
        };
        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();

        await SendToUser(message.SenderId, notification);
    }

    public async Task SendGroupInviteNotificationAsync(Guid conversationId, Guid adminId, Guid invitedUserId, string groupName)
    {
        var admin = await _userRepo.GetByIdAsync(adminId);
        var notification = new Notification
        {
            UserId = invitedUserId,
            Type = "GroupInvite",
            Title = "Added to group",
            Body = $"{admin?.Username} added you to {groupName}",
            ReferenceId = conversationId
        };
        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();

        await SendToUser(invitedUserId, notification);
    }

    public async Task SendKickedNotificationAsync(Guid conversationId, Guid adminId, Guid kickedUserId, string groupName)
    {
        var admin = await _userRepo.GetByIdAsync(adminId);
        var notification = new Notification
        {
            UserId = kickedUserId,
            Type = "System",
            Title = "Removed from group",
            Body = $"{admin?.Username} removed you from {groupName}",
            ReferenceId = conversationId
        };
        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();

        await SendToUser(kickedUserId, notification);
    }

    // ✅ Deliver pending notifications when user comes online
    public async Task DeliverPendingNotificationsAsync(Guid userId)
    {
        var pendingNotifications = await _context.PendingNotifications
            .Where(p => p.UserId == userId && !p.IsDelivered)
            .ToListAsync();

        if (pendingNotifications.Count > 0)
        {
            foreach (var pending in pendingNotifications)
            {
                var notificationDto = new NotificationDto(
                    Guid.NewGuid(),
                    pending.Type,
                    pending.Title,
                    pending.Body,
                    pending.ReferenceId,
                    false,
                    pending.CreatedAt
                );

                await SendRealTimeNotification(userId, notificationDto);
                pending.IsDelivered = true;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"📬 Delivered {pendingNotifications.Count} pending notifications to user: {userId}");
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, int skip, int take)
    {
        var notifications = await _notificationRepo.GetUserNotificationsAsync(userId, skip, take);
        return notifications.Select(n => new NotificationDto(
            n.NotificationId,
            n.Type,
            n.Title,
            n.Body,
            n.ReferenceId,
            n.IsRead,
            n.CreatedAt
        ));
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepo.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        await _notificationRepo.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _notificationRepo.MarkAllAsReadAsync(userId);
    }

    // ─── PRIVATE HELPERS ─────────────────────

    private async Task SendToUser(Guid userId, Notification notification)
    {
        var isOnline = _connectionManager.IsUserOnline(userId);
       
        if (isOnline)
        {
            await SendRealTimeNotification(userId, notification);
        }
        else
        {
            await StorePendingNotification(userId, notification);
            await SendPushToUserDevices(userId, notification.Title, notification.Body ?? "");
        }
    }

    private async Task SendRealTimeNotification(Guid userId, NotificationDto dto)
    {
        var connections = _connectionManager.GetConnections(userId);
        foreach (var connectionId in connections)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("NewNotification", dto);
        }
    }

    private async Task SendRealTimeNotification(Guid userId, Notification notification)
    {
        var dto = new NotificationDto(
            notification.NotificationId,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedAt
        );
        await SendRealTimeNotification(userId, dto);
    }

    private async Task StorePendingNotification(Guid userId, Notification notification)
    {
        var pending = new PendingNotification
        {
            UserId = userId,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            ReferenceId = notification.ReferenceId,
            IsDelivered = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.PendingNotifications.AddAsync(pending);
    }

    private async Task SendPushToUserDevices(Guid userId, string title, string body, string? conversationId = null)
    {
        var devices = await _context.UserDevices
            .Where(d => d.UserId == userId)
            .Select(d => d.DeviceToken)
            .ToListAsync();

        if (devices.Count > 0)
        {
            await _fcmService.SendMulticastAsync(devices, title, body, conversationId);
        }
    }

    private async Task<IEnumerable<Guid>> GetConversationParticipants(Guid conversationId)
    {
        var participants = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
        return participants.Select(p => p.UserId).ToList();
    }
}