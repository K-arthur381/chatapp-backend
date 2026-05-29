using ChatApp.Api.DTOs;
using ChatApp.Api.Hubs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[Authorize]
public class ChatHub : Hub
{
    private readonly ConnectionManager _connections;
    private readonly IChatService _chatService;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notification;

    public ChatHub(ConnectionManager connections, IChatService chatService, IUserRepository userRepo, INotificationService notification)
    {
        _connections = connections;
        _chatService = chatService;
        _userRepo = userRepo;
        _notification = notification;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _connections.AddConnection(userId, Context.ConnectionId);
        await SetUserOnline(userId, true);
        await Clients.Others.SendAsync("UserOnline", userId);

        var convIds = await _chatService.GetUserConversationIds(userId);
        foreach (var id in convIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv-{id}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = GetUserId();
        _connections.RemoveConnection(userId, Context.ConnectionId);
        if (!_connections.GetConnections(userId).Any())
        {
            await SetUserOnline(userId, false);
            await Clients.Others.SendAsync("UserOffline", userId);
        }
        await base.OnDisconnectedAsync(ex);
    }

    public async Task SendMessage(SendMessageDto dto)
    {
        var userId = GetUserId();
        var message = await _chatService.SaveMessage(userId, dto);
        await Clients.Group($"conv-{dto.ConversationId}").SendAsync("NewMessage", message);
        // Trigger notification (fire and forget)
        _ = _notification.SendMessageNotification(new Message { MessageId = message.MessageId });
    }

    public async Task TypingIndicator(Guid conversationId, bool isTyping)
    {
        var user = await _userRepo.GetByIdAsync(GetUserId());
        await Clients.OthersInGroup($"conv-{conversationId}").SendAsync("UserTyping", new { user = new { user!.UserId, user.Username }, isTyping });
    }

    public async Task MarkAsRead(Guid messageId)
    {
        var userId = GetUserId();
        var msg = await _chatService.MarkMessageAsRead(messageId, userId);
        await Clients.Group($"conv-{msg.ConversationId}").SendAsync("MessageRead", new { messageId, userId , conversationId = msg.ConversationId });
    }

    public async Task ReactToMessage(Guid messageId, string reaction)
    {
        var userId = GetUserId();
        var reactionDto = await _chatService.ToggleReaction(messageId, userId, reaction);
        await Clients.Group($"conv-{reactionDto.ConversationId}").SendAsync("MessageReaction", reactionDto);
    }

    private Guid GetUserId() => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task SetUserOnline(Guid userId, bool online)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null) return;
        user.IsOnline = online;
        if (!online) user.LastSeen = DateTime.Now;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }
}