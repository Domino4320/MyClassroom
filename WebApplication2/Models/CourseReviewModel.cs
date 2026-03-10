using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class CourseReviewModel
    {
        public int Id { get; set; }

        // Связь с курсом
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public CourseModel Course { get; set; }

        // Связь с пользователем через Login
        public string UserLogin { get; set; }
        [ForeignKey("UserLogin")]
        public UserModel User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // Оценка от 1 до 5

        [Required]
        [StringLength(1000)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRecommended { get; set; }
    }
}