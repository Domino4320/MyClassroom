using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class NotificationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserLogin { get; set; } = string.Empty;

        [ForeignKey(nameof(UserLogin))]
        public UserModel User { get; set; } = null!;

        [Required]
        [StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Body { get; set; }

        [StringLength(512)]
        public string? Url { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

