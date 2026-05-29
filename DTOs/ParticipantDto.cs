namespace ChatApp.Api.DTOs
{
    public record ParticipantDto(
     Guid UserId,
     string Username,
     string? AvatarUrl,
     bool IsOnline,
     DateTime? LastSeen,
     string Role
 );
}
