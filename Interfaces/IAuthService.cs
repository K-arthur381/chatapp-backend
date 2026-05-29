using ChatApp.Api.Models;
using ChatApp.Api.DTOs;

namespace ChatApp.Api.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
    }
}
