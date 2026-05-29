using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Repositories;
using ChatApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Repositories;

public class ParticipantRepository : GenericRepository<Participant>, IParticipantRepository
{
    public ParticipantRepository(AppDbContext context) : base(context) { }

    public async Task<bool> IsUserInConversationAsync(Guid userId, Guid conversationId)
    {
        return await _dbSet.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
    }

    public async Task<IReadOnlyList<Participant>> GetParticipantsByConversationAsync(Guid conversationId)
    {
        return await _dbSet
            .Where(p => p.ConversationId == conversationId)
            .Include(p => p.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Participant?> GetByUserAndConversationAsync(Guid userId, Guid conversationId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);
    }
}