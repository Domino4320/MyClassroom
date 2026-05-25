using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class CourseBookmarkModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserLogin { get; set; } = string.Empty;

        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public CourseModel Course { get; set; } = null!;

        public int StepId { get; set; }

        [ForeignKey(nameof(StepId))]
        public StepModel Step { get; set; } = null!;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
