using ChatApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Api.Data.Configurations
{
    public class MessageReadStatusConfiguration : IEntityTypeConfiguration<MessageReadStatus>
    {
        public void Configure(EntityTypeBuilder<MessageReadStatus> builder)
        {
            builder.ToTable("MessageReadStatus");
            builder.HasKey(rs => rs.ReadStatusId);
            builder.HasIndex(rs => new { rs.MessageId, rs.UserId }).IsUnique();
            builder.HasOne(rs => rs.Message).WithMany(m => m.ReadStatuses).HasForeignKey(rs => rs.MessageId);
            builder.HasOne(rs => rs.User).WithMany(u => u.ReadStatuses).HasForeignKey(rs => rs.UserId);
        }
    }
}
