using ChatApp.Api.DTOs;
using ChatApp.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatApp.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class MessagesController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IMessageRepository _messageRepo;
    private readonly IMessageReadStatusRepository _readStatusRepo;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IFileStorageService _fileStorage;

    public MessagesController(IChatService chatService, IFileStorageService fileStorage, IMessageRepository messageRepo, IHubContext<ChatHub> hubContext, IMessageReadStatusRepository messageReadStatusRepository)
    {
        _chatService = chatService;
        _fileStorage = fileStorage;
        _messageRepo = messageRepo;
        _hubContext = hubContext;
        _readStatusRepo = messageReadStatusRepository;
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessages(Guid conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var messages = await _chatService.LoadMessages(conversationId, skip, take);
        return Ok(messages);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<IEnumerable<AttachmentDto>>> UploadFiles([FromQuery] Guid conversationId, List<IFormFile> files)
    {
        var userId = GetUserId();
        var attachments = new List<AttachmentDto>();
        foreach (var file in files)
        {
            var url = await _fileStorage.UploadAsync(file, userId.ToString());
            attachments.Add(new AttachmentDto(url, file.FileName, file.ContentType, file.Length, null, null));
        }
        return Ok(attachments);
    }

    [HttpGet("{messageId}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(Guid messageId)
    {
        var messages = await _chatService.GetMessageById(messageId);
        return Ok(messages);
    }

    [HttpPost("conversation/{conversationId}/mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead(Guid conversationId)
    {
        var userId = GetUserId();
        await _readStatusRepo.MarkAllAsReadAsync(conversationId, userId);

        return Ok();
    }


    [HttpPut("{messageId}")]
    public async Task<ActionResult> EditMessage(Guid messageId, [FromBody] EditMessageDto dto)
    {
        var userId = GetUserId();
        var message = await _messageRepo.GetByIdAsync(messageId);

        if (message == null) return NotFound();
        if (message.SenderId != userId) return Forbid();
        if (message.IsDeleted) return BadRequest("Message is deleted");

        message.Content = dto.Content;
        message.EditedAt = DateTime.UtcNow;
        _messageRepo.Update(message);
        await _messageRepo.SaveChangesAsync();

        // Notify group via SignalR
        await _hubContext.Clients.Group($"conv-{message.ConversationId}")
            .SendAsync("MessageEdited", new { messageId, content = dto.Content, editedAt = message.EditedAt });

        return Ok();
    }

    [HttpDelete("{messageId}")]
    public async Task<ActionResult> DeleteMessage(Guid messageId)
    {
        var userId = GetUserId();
        var message = await _messageRepo.GetByIdAsync(messageId);

        if (message == null) return NotFound();
        if (message.SenderId != userId) return Forbid();

        message.IsDeleted = true;
        message.Content = "This message was deleted";
        _messageRepo.Update(message);
        await _messageRepo.SaveChangesAsync();

        // Notify group via SignalR
        await _hubContext.Clients.Group($"conv-{message.ConversationId}")
            .SendAsync("MessageDeleted", new { messageId });

        return Ok();
    }

    public record EditMessageDto(string Content);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}