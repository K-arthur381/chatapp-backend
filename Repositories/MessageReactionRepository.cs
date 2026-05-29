using ChatApp.Api.Data;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Repositories;
using Microsoft.EntityFrameworkCore;

public class MessageReactionRepository : GenericRepository<MessageReaction>, IMessageReactionRepository
{
    public MessageReactionRepository(AppDbContext context) : base(context) { }

    public async Task<MessageReaction?> GetByMessageAndUserAsync(Guid messageId, Guid userId)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
    }

    public async Task<IList<MessageReaction>> GetReactionsForMessageAsync(Guid messageId)
    {
        return await _dbSet.Where(r => r.MessageId == messageId).Include(r => r.User).ToListAsync();
    }
}