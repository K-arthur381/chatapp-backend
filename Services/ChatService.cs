using ChatApp.Api.DTOs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using Microsoft.Extensions.Logging;

namespace ChatApp.Server.Services;

public class ChatService : IChatService
{
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationRepository _conversationRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IMessageReactionRepository _reactionRepo;
    private readonly IMessageReadStatusRepository _readRepo;
    private readonly IAttachmentRepository _attachmentRepo;
    private readonly IUserRepository _userRepo;
    
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IMessageRepository messageRepo,
        IConversationRepository conversationRepo,
        IParticipantRepository participantRepo,
        IMessageReactionRepository reactionRepo,
        IMessageReadStatusRepository readRepo,
        IUserRepository userRepo,
        IAttachmentRepository attachmentRepo,
        ILogger<ChatService> logger)
    {
        _messageRepo = messageRepo;
        _conversationRepo = conversationRepo;
        _participantRepo = participantRepo;
        _reactionRepo = reactionRepo;
        _readRepo = readRepo;
        _attachmentRepo = attachmentRepo;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Guid>> GetUserConversationIds(Guid userId)
    {
        var conversations = await _conversationRepo.GetUserConversationsAsync(userId);
        return conversations.Select(c => c.ConversationId);
    }

    //public async Task<MessageDto> SaveMessage(Guid senderId, SendMessageDto dto)
    //{
    //    if (!await _participantRepo.IsUserInConversationAsync(senderId, dto.ConversationId))
    //        throw new UnauthorizedAccessException("User not in conversation");

    //    var message = new Message
    //    {
    //        ConversationId = dto.ConversationId,
    //        SenderId = senderId,
    //        Content = dto.Content,
    //        MessageType = dto.MessageType,
    //        ReplyToMessageId = dto.ReplyToMessageId,
    //        ForwardedFromUserId = dto.ForwardFromMessageId != null
    //            ? (await _messageRepo.GetByIdAsync(dto.ForwardFromMessageId.Value))?.SenderId
    //            : null
    //    };

    //    await _messageRepo.AddAsync(message);
    //    await _messageRepo.SaveChangesAsync();

    //    // Mark sender as read
    //    var read = new MessageReadStatus { MessageId = message.MessageId, UserId = senderId };
    //    await _readRepo.AddAsync(read);
    //    await _readRepo.SaveChangesAsync();

    //    return await MapToMessageDto(message, senderId);
    //}

    public async Task<MessageDto> SaveMessage(Guid senderId, SendMessageDto dto)
    {
        if (!await _participantRepo.IsUserInConversationAsync(senderId, dto.ConversationId))
            throw new UnauthorizedAccessException("User not in conversation");

        var message = new Message
        {
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            Content = dto.MessageType == "Text" ? dto.Content : null,
            MessageType = dto.MessageType,
            ReplyToMessageId = dto.ReplyToMessageId,
            ForwardedFromUserId = dto.ForwardFromMessageId != null
                ? (await _messageRepo.GetByIdAsync(dto.ForwardFromMessageId.Value))?.SenderId
                : null
        };

        await _messageRepo.AddAsync(message);
        await _messageRepo.SaveChangesAsync();

        // ✅ Handle multiple attachments
        if (dto.Attachments != null && dto.Attachments.Count > 0)
        {
            foreach (var att in dto.Attachments)
            {
                var attachment = new Attachment
                {
                    MessageId = message.MessageId,
                    FileUrl = att.FileUrl,
                    FileName = att.FileName ?? "file",
                    ContentType = att.ContentType,
                    FileSize = att.FileSize,
                    ThumbnailUrl = att.ContentType.StartsWith("image/") ? att.FileUrl : null
                };
                await _attachmentRepo.AddAsync(attachment);
            }
            await _attachmentRepo.SaveChangesAsync();
        }
        // ✅ Fallback: single attachment from old format
        else if ((dto.MessageType == "Image" || dto.MessageType == "File") && !string.IsNullOrEmpty(dto.Content))
        {
            var attachment = new Attachment
            {
                MessageId = message.MessageId,
                FileUrl = dto.Content,
                FileName = dto.FileName ?? "file",
                ContentType = dto.MessageType == "Image" ? "image/jpeg" : "application/octet-stream",
                FileSize = dto.FileSize,
                ThumbnailUrl = dto.MessageType == "Image" ? dto.Content : null
            };
            await _attachmentRepo.AddAsync(attachment);
            await _attachmentRepo.SaveChangesAsync();
        }

        // Mark sender as read
        var read = new MessageReadStatus { MessageId = message.MessageId, UserId = senderId };
        await _readRepo.AddAsync(read);
        await _readRepo.SaveChangesAsync();

        var messageInfo = await _messageRepo.GetByMessageIdAsync(message.MessageId);

        return await MapToMessageDto(messageInfo, senderId);
    }
    public async Task<MessageDto> MarkMessageAsRead(Guid messageId, Guid userId)
    {
        var message = await _messageRepo.GetMessageWithDetailsAsync(messageId);
        if (message is null) throw new KeyNotFoundException("Message not found");

        if (!await _participantRepo.IsUserInConversationAsync(userId, message.ConversationId))
            throw new UnauthorizedAccessException();

        var exists = await _readRepo.GetByMessageAndUserAsync(messageId, userId);
        if (exists is null)
        {
            await _readRepo.AddAsync(new MessageReadStatus { MessageId = messageId, UserId = userId });
            await _readRepo.SaveChangesAsync();
        }

        return await MapToMessageDto(message, userId);
    }

    public async Task<ReactionDto> ToggleReaction(Guid messageId, Guid userId, string reaction)
    {
        var message = await _messageRepo.GetMessageWithDetailsAsync(messageId)
                      ?? throw new KeyNotFoundException("Message not found");
        if (!await _participantRepo.IsUserInConversationAsync(userId, message.ConversationId))
            throw new UnauthorizedAccessException();

        var existing = await _reactionRepo.GetByMessageAndUserAsync(messageId, userId);
        if (existing is not null)
        {
            if (existing.Reaction == reaction)
                _reactionRepo.Delete(existing);
            else
                existing.Reaction = reaction;
        }
        else
        {
            await _reactionRepo.AddAsync(new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Reaction = reaction
            });
        }
        await _reactionRepo.SaveChangesAsync();

        var user = await _userRepo.GetByIdAsync(userId);
        return new ReactionDto(messageId, message.ConversationId, userId, user!.Username, reaction);
    }

    public async Task<IEnumerable<MessageDto>> LoadMessages(Guid conversationId, int skip, int take)
    {
        var messages = await _messageRepo.GetMessagesByConversationAsync(conversationId, skip, take);
        var participantIds = await _conversationRepo.GetParticipantIdsAsync(conversationId);
        int total = participantIds.Count;

        var result = new List<MessageDto>();
        foreach (var m in messages)
        {
            result.Add(await MapToMessageDto(m, Guid.Empty)); // for list without current user reaction
        }
        return result;
    }

    public async Task<MessageDto?> GetMessageById(Guid messageId)
    {
        var message = await _messageRepo.GetByMessageIdAsync(messageId);

        if (message == null)
            return null;

        var result = await MapToMessageDto(message, Guid.Empty);

        return result;
    }

    private async Task<MessageDto> MapToMessageDto(Message message, Guid currentUserId)
    {
        var sender = await _userRepo.GetByIdAsync(message.SenderId);
        var reactions = await _reactionRepo.GetReactionsForMessageAsync(message.MessageId);
        var readStatuses = await _readRepo.GetReadStatusesForMessageAsync(message.MessageId);
        var participantCount = (await _conversationRepo.GetParticipantIdsAsync(message.ConversationId)).Count;

        var reactionCounts = reactions.GroupBy(r => r.Reaction)
                                     .ToDictionary(g => g.Key, g => g.Count());
        var currentReaction = reactions.FirstOrDefault(r => r.UserId == currentUserId)?.Reaction;

        // ✅ Map all attachments
        var attachmentDtos = message.Attachments.Select(a => new AttachmentDto(
            a.FileUrl,
            a.FileName,
            a.ContentType ?? "application/octet-stream",
            a.FileSize,
            a.ThumbnailUrl,
            a.DurationSeconds
        ));

        return new MessageDto(
            message.MessageId,
            message.ConversationId,
            new UserDto(sender!.UserId, sender.Username, sender.Email, sender.AvatarUrl, sender.IsOnline, sender.LastSeen),
            message.Content,
            message.MessageType,
            message.SentAt,
            message.ReplyToMessageId,
            message.ReplyToMessage is not null
                ? new ReplyInfoDto(message.ReplyToMessage.MessageId, message.ReplyToMessage.Content, message.ReplyToMessage.Sender!.Username)
                : null,
            message.ForwardedFromUser is not null
                ? new UserDto(message.ForwardedFromUser.UserId, message.ForwardedFromUser.Username, message.ForwardedFromUser.Email, message.ForwardedFromUser.AvatarUrl, message.ForwardedFromUser.IsOnline, message.ForwardedFromUser.LastSeen)
                : null,
            attachmentDtos,
            reactionCounts,
            currentReaction,
            readStatuses.Count,
            participantCount
        );
    }

   
}