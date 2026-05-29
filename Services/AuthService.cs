using ChatApp.Api.DTOs;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatApp.Server.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly IFileStorageService _fileStorage;

    public AuthService(IUserRepository userRepo, IConfiguration config, IFileStorageService fileStorage)
    {
        _userRepo = userRepo;
        _config = config;
        _fileStorage = fileStorage;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepo.GetByEmailAsync(dto.Email) is not null) return null;
        var hasher = new PasswordHasher<User>();
        // Save avatar file if provided
        string? avatarUrl = null;
        if (dto.AvatarFile != null)
        {
            avatarUrl = await _fileStorage.UploadAsync(dto.AvatarFile, "avatars");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AvatarUrl = avatarUrl
        };

        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        var token = GenerateToken(user);
        return new AuthResponseDto(token, MapUserDto(user));
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email);
        if (user is null) return null;
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed) return null;
        var token = GenerateToken(user);
        return new AuthResponseDto(token, MapUserDto(user));
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user is null ? null : MapUserDto(user);
    }

    private string GenerateToken(User user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapUserDto(User user) =>
        new(user.UserId, user.Username, user.Email, user.AvatarUrl, user.IsOnline, user.LastSeen);
}