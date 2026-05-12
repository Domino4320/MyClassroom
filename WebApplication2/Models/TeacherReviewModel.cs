using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class TeacherReviewModel
    {
        public int Id { get; set; }

        [Required]
        public string TeacherLogin { get; set; }

        [ForeignKey("TeacherLogin")]
        public TeacherProfile Teacher { get; set; }

        [Required]
        public string UserLogin { get; set; }

        [ForeignKey("UserLogin")]
        public UserModel User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRecommended { get; set; }
    }
}
