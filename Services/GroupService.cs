using ChatApp.Api.DTOs;
using ChatApp.Api.Hubs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ChatApp.Api.Services
{
    public class GroupService : IGroupService
    {
        private readonly IConversationRepository _convRepo;
        private readonly IParticipantRepository _participantRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IUserRepository _userRepo;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ConnectionManager _connectionManager;

        public GroupService(
            IConversationRepository convRepo,
            IParticipantRepository participantRepo,
            IMessageRepository messageRepo,
            IUserRepository userRepo,
            IHubContext<ChatHub> hubContext,
            ConnectionManager connectionManager)
        {
            _convRepo = convRepo;
            _participantRepo = participantRepo;
            _messageRepo = messageRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
        }

        public async Task AddMemberAsync(Guid adminId, Guid conversationId, Guid userId)
        {
            var conv = await _convRepo.GetByIdAsync(conversationId);
            if (conv == null || conv.Type != "Group")
                throw new InvalidOperationException("Not a group conversation");

            var adminPart = await _participantRepo.GetByUserAndConversationAsync(adminId, conversationId);
            if (adminPart == null || adminPart.Role != "Admin")
                throw new UnauthorizedAccessException("Only admins can add members");

            if (await _participantRepo.IsUserInConversationAsync(userId, conversationId))
                throw new InvalidOperationException("User already in group");

            // Add new member
            await _participantRepo.AddAsync(new Participant
            {
                ConversationId = conversationId,
                UserId = userId,
                Role = "Member"
            });
            await _participantRepo.SaveChangesAsync();

            // System message
            var admin = await _userRepo.GetByIdAsync(adminId);
            var addedUser = await _userRepo.GetByIdAsync(userId);
            var sysMsg = new Message
            {
                ConversationId = conversationId,
                SenderId = adminId,
                Content = $"{admin!.Username} added {addedUser!.Username}",
                MessageType = "Text",
                IsSystemMessage = true
            };
            await _messageRepo.AddAsync(sysMsg);
            await _messageRepo.SaveChangesAsync();

            // Broadcast updated participant list to the whole group
            await SendGroupParticipantsUpdate(conversationId);

            // Send the system message as a NewMessage
            var participantsCount = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
            var msgDto = MapMessageToDto(sysMsg, admin, addedUser, participantsCount.Count);
            await _hubContext.Clients.Group($"conv-{conversationId}").SendAsync("NewMessage", msgDto);

            // Notify the added user that a new conversation appeared
            var convDto = await BuildConversationDto(conversationId);
            if (convDto != null)
            {
                var connections = _connectionManager.GetConnections(userId);
                foreach (var cid in connections)
                {
                    await _hubContext.Clients.Client(cid).SendAsync("NewConversation", convDto);
                    await _hubContext.Groups.AddToGroupAsync(cid, $"conv-{conversationId}");
                }
                    
            }

        }

        public async Task RemoveMemberAsync(Guid adminId, Guid conversationId, Guid userId)
        {
            var conv = await _convRepo.GetByIdAsync(conversationId);
            if (conv == null || conv.Type != "Group")
                throw new InvalidOperationException("Not a group conversation");

            var adminPart = await _participantRepo.GetByUserAndConversationAsync(adminId, conversationId);
            if (adminPart == null || adminPart.Role != "Admin")
                throw new UnauthorizedAccessException("Only admins can remove members");

            if (userId == adminId)
                throw new InvalidOperationException("Admin cannot remove themselves");

            var participant = await _participantRepo.GetByUserAndConversationAsync(userId, conversationId);
            if (participant == null)
                throw new InvalidOperationException("User not in group");

            _participantRepo.Delete(participant);
            await _participantRepo.SaveChangesAsync();

            // System message
            var admin = await _userRepo.GetByIdAsync(adminId);
            var removedUser = await _userRepo.GetByIdAsync(userId);
            var sysMsg = new Message
            {
                ConversationId = conversationId,
                SenderId = adminId,
                Content = $"{admin!.Username} removed {removedUser!.Username}",
                MessageType = "Text",
                IsSystemMessage = true
            };
            await _messageRepo.AddAsync(sysMsg);
            await _messageRepo.SaveChangesAsync();

            // Broadcast updated participants to the group
            await SendGroupParticipantsUpdate(conversationId);

            // System message as NewMessage
            var participantsCount = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
            var msgDto = MapMessageToDto(sysMsg, admin, removedUser, participantsCount.Count);
            await _hubContext.Clients.Group($"conv-{conversationId}").SendAsync("NewMessage", msgDto);

            // Notify the removed user that the conversation is gone
            var connections = _connectionManager.GetConnections(userId);
            foreach (var cid in connections)
                await _hubContext.Clients.Client(cid).SendAsync("ConversationRemoved", conversationId.ToString());
        }

        public async Task<IEnumerable<ParticipantDto>> GetParticipantsAsync(Guid conversationId)
        {
            var participants = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
            return participants.Select(p => new ParticipantDto(
                p.User.UserId,
                p.User.Username,
                p.User.AvatarUrl,
                p.User.IsOnline,
                p.User.LastSeen,
                p.Role
            ));
        }

        // ─── private helpers ─────────────────────
        private async Task SendGroupParticipantsUpdate(Guid conversationId)
        {
            var participants = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
            var dtos = participants.Select(p => new ParticipantDto(
                p.User.UserId, p.User.Username, p.User.AvatarUrl, p.User.IsOnline, p.User.LastSeen, p.Role
            ));
            await _hubContext.Clients.Group($"conv-{conversationId}").SendAsync("GroupMembersUpdated", new
            {
                ConversationId = conversationId,
                Participants = dtos
            });
        }

        private async Task<ConversationDto?> BuildConversationDto(Guid conversationId)
        {
            var conv = await _convRepo.GetByIdAsync(conversationId);
            if (conv == null) return null;
            var parts = await _participantRepo.GetParticipantsByConversationAsync(conversationId);
            var userDtos = parts.Select(p => new UserDto(
                p.User.UserId, p.User.Username, p.User.Email, p.User.AvatarUrl, p.User.IsOnline, p.User.LastSeen
            )).ToList();
            return new ConversationDto(conv.ConversationId, conv.Type, conv.GroupName, conv.GroupAvatarUrl, userDtos, null);
        }

        private MessageDto MapMessageToDto(Message m, User sender, User? targetUser, int totalParticipants)
        {
            return new MessageDto(
                m.MessageId,
                m.ConversationId,
                new UserDto(sender.UserId, sender.Username, sender.Email, sender.AvatarUrl, sender.IsOnline, sender.LastSeen),
                m.Content,
                m.MessageType,
                m.SentAt,
                m.ReplyToMessageId,
                null, // reply
                null, // forward
                new List<AttachmentDto>(),
                new Dictionary<string, int>(),
                null,
                0,
                totalParticipants
            );
        }
    }
}
