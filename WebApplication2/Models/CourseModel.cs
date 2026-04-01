using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public enum StepType
    {
        Text,
        Video,
        Quiz,
        Code
    }

    public class CourseModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название курса")]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string Category { get; set; }

        public string? CoverImagePath { get; set; }

        [Required]
        public string? AuthorLogin { get; set; }

        [ForeignKey("AuthorLogin")]
        public TeacherProfile? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ModuleModel> Modules { get; set; } = new();

        [Required]
        public bool IsPublished { get; set; } = false;
        public List<CourseReviewModel> Reviews { get; set; } = new List<CourseReviewModel>();

        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any()
            ? Math.Round(Reviews.Average(r => r.Rating), 1)
            : 0;

        [NotMapped]
        public int ReviewsCount => Reviews?.Count ?? 0;
    }

    public class ModuleModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название модуля обязательно")]
        public string Title { get; set; }

        public int Order { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public CourseModel Course { get; set; }

        public List<LessonModel> Lessons { get; set; } = new();
    }

    public class LessonModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public int Order { get; set; }

        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public ModuleModel Module { get; set; }

        public List<StepModel> Steps { get; set; } = new();
    }

    public class StepModel
    {
        [Key]
        public int Id { get; set; }

        public string? Title { get; set; }

        [Required]
        public StepType Type { get; set; }

        public int Order { get; set; }

        // --- ПОЛЯ КОНТЕНТА ---
        public string? TextContent { get; set; }
        public string? VideoUrl { get; set; }
        public string? CodeTemplate { get; set; }
        public string? ExpectedOutput { get; set; }

        // --- ЛОГИКА ТЕСТОВ ---
        [Required]
        public bool IsMultipleChoice { get; set; } = false;

        // НОВОЕ: Тип проверки для Quiz (True - вручную учителем, False - автоматически системой)
        public bool IsManualCheck { get; set; } = false;

        // НОВОЕ: Эталонный ответ для текстовой автопроверки (если применимо)
        public string? CorrectTextAnswer { get; set; }

        // НОВОЕ: Максимальный балл за выполнение этого шага
        public int MaxPoints { get; set; } = 1;

        public int LessonId { get; set; }
        [ForeignKey("LessonId")]
        public LessonModel Lesson { get; set; }

        public List<QuizOptionModel> QuizOptions { get; set; } = new();

        // Связь с ответами пользователей
        public List<StepSubmissionModel> Submissions { get; set; } = new();
    }

    public class QuizOptionModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }

        public int StepId { get; set; }
        [ForeignKey("StepId")]
        public StepModel Step { get; set; }
    }

    // НОВАЯ МОДЕЛЬ: Хранение ответов пользователей на тесты/задания
    public class StepSubmissionModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserLogin { get; set; }

        public int StepId { get; set; }
        [ForeignKey("StepId")]
        public StepModel Step { get; set; }

        // Текст ответа пользователя (для открытых вопросов)
        public string? UserAnswerText { get; set; }

        // Статус проверки
        public bool IsPending { get; set; } = true; // Ожидает ли проверки учителем
        public bool IsCorrect { get; set; } = false; // Пройдено ли успешно

        // Баллы и комментарий
        public int EarnedPoints { get; set; } = 0;
        public string? TeacherComment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserProgressModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserLogin { get; set; }

        public int StepId { get; set; }
        [ForeignKey("StepId")]
        public StepModel Step { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}