using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public enum LessonMaterialKind
    {
        File = 0,
        Link = 1
    }

    public class LessonMaterialModel
    {
        [Key]
        public int Id { get; set; }

        public int LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public LessonModel Lesson { get; set; } = null!;

        public LessonMaterialKind Kind { get; set; }

        /// <summary>Отображаемое название (файл или ссылка).</summary>
        public string Title { get; set; } = "";

        public string? FileName { get; set; }

        public string? StoredPath { get; set; }

        public string? Url { get; set; }

        public int Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LessonFeedbackModel
    {
        [Key]
        public int Id { get; set; }

        public int LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public LessonModel Lesson { get; set; } = null!;

        public string UserLogin { get; set; } = "";

        /// <summary>1–5: сложность.</summary>
        public int Difficulty { get; set; }

        /// <summary>1–5: понятность.</summary>
        public int Clarity { get; set; }

        /// <summary>1–5: интересность.</summary>
        public int Interest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
