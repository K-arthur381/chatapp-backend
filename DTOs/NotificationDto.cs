namespace ChatApp.Api.DTOs
{
    public record NotificationDto(
    Guid NotificationId,
    string Type,
    string Title,
    string? Body,
    Guid? ReferenceId,
    bool IsRead,
    DateTime CreatedAt
);
}
