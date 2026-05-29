using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ChatApp.Api.Data.Configurations
{
  
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");
            builder.HasKey(m => m.MessageId);
            builder.Property(m => m.Content).HasMaxLength(2000);
            builder.Property(m => m.MessageType).HasMaxLength(20).IsRequired();
            builder.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Sender).WithMany(u => u.Messages).HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.ReplyToMessage).WithMany().HasForeignKey(m => m.ReplyToMessageId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(m => m.ForwardedFromUser).WithMany().HasForeignKey(m => m.ForwardedFromUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasIndex(m => new { m.ConversationId, m.SentAt });
        }
    }
}
