using ChatApp.Api.DTOs;

namespace ChatApp.Api.Interfaces
{
    public interface IGroupService
    {
        Task AddMemberAsync(Guid adminId, Guid conversationId, Guid userId);
        Task RemoveMemberAsync(Guid adminId, Guid conversationId, Guid userId);
        Task<IEnumerable<ParticipantDto>> GetParticipantsAsync(Guid conversationId);
    }
}
