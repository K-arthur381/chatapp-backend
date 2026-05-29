using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Repositories;
using ChatApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Repositories;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public async Task<Message?> GetByMessageIdAsync(Guid messageId)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reactions)
                .ThenInclude(r => r.User)
            .Include(m => m.ReadStatuses)
            .Include(m => m.ReplyToMessage!)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ForwardedFromUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesByConversationAsync(Guid conversationId, int skip, int take)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip).Take(take)
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReadStatuses)
            .Include(m => m.ReplyToMessage!).ThenInclude(r => r!.Sender)
            .Include(m => m.ForwardedFromUser)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Message?> GetMessageWithDetailsAsync(Guid messageId)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReadStatuses)
            .Include(m => m.ReplyToMessage!).ThenInclude(r => r!.Sender)
            .Include(m => m.ForwardedFromUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
    }
}