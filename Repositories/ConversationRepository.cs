using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Repositories;
using ChatApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Repositories;

public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.Participants.Any(p => p.UserId == userId) && !c.IsDeleted)
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.Where(msg=>!msg.IsDeleted).OrderByDescending(m => m.SentAt).Take(1)).ThenInclude(m => m.Sender)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetParticipantIdsAsync(Guid conversationId)
    {
        return await _context.Participants
            .Where(p => p.ConversationId == conversationId)
            .Select(p => p.UserId)
            .ToListAsync();
    }
}