using ChatApp.Api.DTOs;
using ChatApp.Api.Hubs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationRepository _convRepo;
        private readonly IParticipantRepository _partRepo;
        private readonly IUserRepository _userRepo;
        private readonly IGroupService _groupService;
        private readonly IMessageReadStatusRepository _readStatusRepo;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ConnectionManager _connManager;

        public ConversationsController(
            IConversationRepository convRepo,
            IParticipantRepository partRepo,
            IUserRepository userRepo,
            IGroupService groupService,
             IMessageReadStatusRepository readStatusRepo,
            IHubContext<ChatHub> hubContext,
            ConnectionManager connManager)
        {
            _convRepo = convRepo;
            _partRepo = partRepo;
            _userRepo = userRepo;
            _groupService = groupService;
            _readStatusRepo = readStatusRepo;
            _hubContext = hubContext;
            _connManager = connManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetMyConversations()
        {
            var userId = GetUserId();
            var convs = await _convRepo.GetUserConversationsAsync(userId);
            var dtos = new List<ConversationDto>();
            foreach (var c in convs)
            {
                var parts = await _partRepo.GetParticipantsByConversationAsync(c.ConversationId);
                var userDtos = parts.Select(p => new UserDto(
                    p.User.UserId, p.User.Username, p.User.Email, p.User.AvatarUrl, p.User.IsOnline, p.User.LastSeen
                )).ToList();
                var lastMsg = c.Messages.Where(msg=> !msg.IsDeleted).OrderByDescending(m => m.SentAt).FirstOrDefault();
                MessageDto? lastDto = null;
                if (lastMsg != null)
                {
                    var sender = await _userRepo.GetByIdAsync(lastMsg.SenderId);
                    lastDto = new MessageDto(
                        lastMsg.MessageId, lastMsg.ConversationId,
                        new UserDto(sender!.UserId, sender.Username, sender.Email, sender.AvatarUrl, sender.IsOnline, sender.LastSeen),
                        lastMsg.Content, lastMsg.MessageType, lastMsg.SentAt,
                        lastMsg.ReplyToMessageId, null, null,
                        new List<AttachmentDto>(),
                        new Dictionary<string, int>(),
                        null,
                        lastMsg.ReadStatuses?.Count ?? 0,
                        parts.Count
                    );
                }

                // ✅ Get unread count
                var unreadCount = await _readStatusRepo.GetUnreadCountAsync(c.ConversationId, userId);

                dtos.Add(new ConversationDto(c.ConversationId, c.Type, c.GroupName, c.GroupAvatarUrl, userDtos, lastDto, unreadCount));
            }
            return Ok(dtos);
        }

        [HttpPost]

        //public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationDto dto)
        //{
        //    var userId = GetUserId();
        //    var conv = new Conversation
        //    {
        //        Type = dto.Type,
        //        GroupName = dto.GroupName,
        //        GroupAvatarUrl = dto.GroupAvatarUrl,
        //        CreatedBy = userId
        //    };
        //    await _convRepo.AddAsync(conv);
        //    await _convRepo.SaveChangesAsync();

        //    var memberIds = dto.MemberIds ?? new List<Guid>();
        //    if (!memberIds.Contains(userId)) memberIds.Add(userId);

        //    foreach (var id in memberIds)
        //    {
        //        await _partRepo.AddAsync(new Participant
        //        {
        //            ConversationId = conv.ConversationId,
        //            UserId = id,
        //            Role = id == userId ? "Admin" : "Member"
        //        });
        //    }
        //    await _partRepo.SaveChangesAsync();

        //    // Build ConversationDto for broadcast
        //    var parts = await _partRepo.GetParticipantsByConversationAsync(conv.ConversationId);
        //    var userDtos = parts.Select(p => new UserDto(
        //        p.User.UserId, p.User.Username, p.User.Email, p.User.AvatarUrl, p.User.IsOnline, p.User.LastSeen
        //    )).ToList();
        //    var convDto = new ConversationDto(conv.ConversationId, conv.Type, conv.GroupName, conv.GroupAvatarUrl, userDtos, null);

        //    // Broadcast to all participants (new conversation appears instantly)
        //    foreach (var memberId in memberIds)
        //    {
        //        var connections = _connManager.GetConnections(memberId);
        //        foreach (var cid in connections)
        //            await _hubContext.Clients.Client(cid).SendAsync("NewConversation", convDto);
        //    }

        //    return Ok(convDto);
        //}

        public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationDto dto)
        {
            var userId = GetUserId();
            var conv = new Conversation
            {
                Type = dto.Type,
                GroupName = dto.GroupName,
                GroupAvatarUrl = dto.GroupAvatarUrl,
                CreatedBy = userId
            };
            await _convRepo.AddAsync(conv);
            await _convRepo.SaveChangesAsync();

            var memberIds = dto.MemberIds ?? new List<Guid>();
            if (!memberIds.Contains(userId)) memberIds.Add(userId);

            foreach (var id in memberIds)
            {
                await _partRepo.AddAsync(new Participant
                {
                    ConversationId = conv.ConversationId,
                    UserId = id,
                    Role = id == userId ? "Admin" : "Member"
                });
            }
            await _partRepo.SaveChangesAsync();

            // ✅ Add all online members to the SignalR group
            foreach (var memberId in memberIds)
            {
                var connections = _connManager.GetConnections(memberId);
                foreach (var connectionId in connections)
                {
                    await _hubContext.Groups.AddToGroupAsync(connectionId, $"conv-{conv.ConversationId}");
                }
            }

            // Build ConversationDto for broadcast
            var parts = await _partRepo.GetParticipantsByConversationAsync(conv.ConversationId);
            var userDtos = parts.Select(p => new UserDto(
                p.User.UserId, p.User.Username, p.User.Email, p.User.AvatarUrl, p.User.IsOnline, p.User.LastSeen
            )).ToList();
            var convDto = new ConversationDto(conv.ConversationId, conv.Type, conv.GroupName, conv.GroupAvatarUrl, userDtos, null, 0);

            // Broadcast to all participants (new conversation appears instantly)
            foreach (var memberId in memberIds)
            {
                var connections = _connManager.GetConnections(memberId);
                foreach (var cid in connections)
                    await _hubContext.Clients.Client(cid).SendAsync("NewConversation", convDto);
            }

            return Ok(convDto);
        }

        [HttpGet("{conversationId}/participants")]
        public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetParticipants(Guid conversationId)
        {
            if (!await _partRepo.IsUserInConversationAsync(GetUserId(), conversationId))
                return Forbid();
            return Ok(await _groupService.GetParticipantsAsync(conversationId));
        }

        [HttpPost("{conversationId}/members")]
        public async Task<ActionResult> AddMember(Guid conversationId, [FromBody] AddMemberDto dto)
        {
            try
            {
                await _groupService.AddMemberAsync(GetUserId(), conversationId, dto.UserId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{conversationId}/members/{userId}")]
        public async Task<ActionResult> RemoveMember(Guid conversationId, Guid userId)
        {
            try
            {
                await _groupService.RemoveMemberAsync(GetUserId(), conversationId, userId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public record CreateConversationDto(string Type, string? GroupName, List<Guid>? MemberIds,string? GroupAvatarUrl = null);
    public record AddMemberDto(Guid UserId);
}
