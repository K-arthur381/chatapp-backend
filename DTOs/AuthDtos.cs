namespace ChatApp.Api.DTOs
{
  
    public record RegisterDto(
        string Username, 
        string Email,
        IFormFile AvatarFile,
        string Password,
        string FirstName,
        string LastName
        );


    public record LoginDto(
        string Email, 
        string Password
        );


    public record AuthResponseDto(
        string Token, 
        UserDto User
        );


    public record UserDto(
        Guid UserId,
        string Username, 
        string Email,
        string? AvatarUrl,
        bool IsOnline, 
        DateTime? LastSeen
        );
}
