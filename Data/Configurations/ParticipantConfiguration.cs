using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Api.Data.Configurations
{
    public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
    {
        public void Configure(EntityTypeBuilder<Participant> builder)
        {
            builder.ToTable("Participants");
            builder.HasKey(p => p.ParticipantId);
            builder.HasIndex(p => new { p.ConversationId, p.UserId }).IsUnique();
            builder.HasOne(p => p.Conversation).WithMany(c => c.Participants).HasForeignKey(p => p.ConversationId);
            builder.HasOne(p => p.User).WithMany(u => u.Participants).HasForeignKey(p => p.UserId);
        }
    }
}