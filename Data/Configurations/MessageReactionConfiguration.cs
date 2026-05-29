using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Api.Data.Configurations
{
    public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> builder)
        {
            builder.ToTable("MessageReactions");
            builder.HasKey(r => r.ReactionId);
            builder.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
            builder.HasOne(r => r.Message).WithMany(m => m.Reactions).HasForeignKey(r => r.MessageId);
            builder.HasOne(r => r.User).WithMany(u => u.Reactions).HasForeignKey(r => r.UserId);
        }
    }
}
