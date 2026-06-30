using ChatApp.Api.DTOs;
using ChatApp.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
            [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetNotificationsAsync(userId, skip, take);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        [HttpPut("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(Guid notificationId)
        {
            var userId = GetUserId();
            await _notificationService.MarkAsReadAsync(notificationId, userId);
            return Ok();
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
