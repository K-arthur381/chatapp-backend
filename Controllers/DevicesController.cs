using ChatApp.Api.Data;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DevicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
    {
        var userId = GetUserId();

        // Remove old token if exists
        var existing = _context.UserDevices
            .FirstOrDefault(d => d.DeviceToken == dto.DeviceToken);
        if (existing != null)
        {
            _context.UserDevices.Remove(existing);
        }

        // Remove all tokens for this user/platform combo
        var oldDevices = _context.UserDevices
            .Where(d => d.UserId == userId && d.Platform == dto.Platform);
        _context.UserDevices.RemoveRange(oldDevices);

        var device = new UserDevice
        {
            UserId = userId,
            DeviceToken = dto.DeviceToken,
            Platform = dto.Platform,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _context.UserDevices.Add(device);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Device registered" });
    }

    [HttpDelete("unregister")]
    public async Task<ActionResult> UnregisterDevice([FromBody] UnregisterDeviceDto dto)
    {
        var userId = GetUserId();
        var device = _context.UserDevices
            .FirstOrDefault(d => d.UserId == userId && d.DeviceToken == dto.DeviceToken);

        if (device != null)
        {
            _context.UserDevices.Remove(device);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public record RegisterDeviceDto(string DeviceToken, string Platform);
public record UnregisterDeviceDto(string DeviceToken);