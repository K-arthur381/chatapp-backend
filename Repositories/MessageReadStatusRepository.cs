using ChatApp.Api.Data;
using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Repositories;
using Microsoft.EntityFrameworkCore;

public class MessageReadStatusRepository : GenericRepository<MessageReadStatus>, IMessageReadStatusRepository
{
    public MessageReadStatusRepository(AppDbContext context) : base(context) { }

    public async Task<MessageReadStatus?> GetByMessageAndUserAsync(Guid messageId, Guid userId)
    {
        return await _dbSet.FirstOrDefaultAsync(rs => rs.MessageId == messageId && rs.UserId == userId);
    }

    public async Task<IList<MessageReadStatus>> GetReadStatusesForMessageAsync(Guid messageId)
    {
        return await _dbSet.Where(rs => rs.MessageId == messageId).ToListAsync();
    }
    //  Mark all messages in a conversation as read
    public async Task MarkAllAsReadAsync(Guid conversationId, Guid userId)
    {
        // Get all unread message IDs
        var unreadMessageIds = await _context.Messages
            .Where(m => m.ConversationId == conversationId
                     && !m.IsDeleted
                     && !m.IsSystemMessage
                     && m.SenderId != userId)  // Don't mark own messages
            .Where(m => !_context.MessageReadStatus
                .Any(rs => rs.MessageId == m.MessageId && rs.UserId == userId))
            .Select(m => m.MessageId)
            .ToListAsync();

        // Create read status for each
        var now = DateTime.UtcNow;
        var readStatuses = unreadMessageIds.Select(messageId => new MessageReadStatus
        {
            MessageId = messageId,
            UserId = userId,
            ReadAt = now
        });

        await _context.MessageReadStatus.AddRangeAsync(readStatuses);
        await _context.SaveChangesAsync();
    }
    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        // total messages in conversation that were NOT read by this user
        var totalMessages = await _context.Messages
            .Where(m => m.ConversationId == conversationId  && !m.IsDeleted && !m.IsSystemMessage)
            .CountAsync();

        var readMessages = await _context.MessageReadStatus
            .Where(rs => rs.Message.ConversationId == conversationId && rs.UserId == userId)
            .CountAsync();

        return totalMessages - readMessages;
    }
}