using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Models
{
    public class UserDevice
    {
        [Key]
        public int Id { get; set; }
        public Guid DeviceId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = "web";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        public User User { get; set; } = null!;
    }
}
