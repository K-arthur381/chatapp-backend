using ChatApp.Api.DTOs;
using ChatApp.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UsersController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var users = await _userRepo.GetAllUsersAsync(currentUserId);

            var dtos = users.Select(u => new UserDto(
                u.UserId,
                u.Username,
                u.Email,
                u.AvatarUrl,
                u.IsOnline,
                u.LastSeen
            ));

            return Ok(dtos);
        }

    }
}
