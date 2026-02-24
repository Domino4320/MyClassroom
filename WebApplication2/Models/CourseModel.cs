using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    // 1. ТИПЫ ШАГОВ (Контент-блоков)
    public enum StepType
    {
        Text,   // Лекция/Теория
        Video,  // Видео-урок
        Quiz,   // Тест с вариантами ответов
        Code    // Задача на программирование
    }

    // 2. КУРС (Уже имеющаяся у вас модель, дополненная связью)
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Связь: Один курс -> Много модулей
        public List<ModuleModel> Modules { get; set; } = new();

        [Required]
        public bool IsPublished { get; set; } = false; // По умолчанию курс — черновик
    }

    // 3. МОДУЛЬ (Глава курса)
    public class ModuleModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название модуля обязательно")]
        public string Title { get; set; }

        public int Order { get; set; } // Порядок в списке

        // Внешний ключ на курс
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public CourseModel Course { get; set; }

        // Связь: Один модуль -> Много уроков
        public List<LessonModel> Lessons { get; set; } = new();
    }

    // 4. УРОК (Страница с набором шагов)
    public class LessonModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public int Order { get; set; }

        // Внешний ключ на модуль
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public ModuleModel Module { get; set; }

        // Связь: Один урок -> Много шагов
        public List<StepModel> Steps { get; set; } = new();
    }

    // 5. ШАГ (Атомарная единица контента)
    public class StepModel
    {
        [Key]
        public int Id { get; set; }

        public string? Title { get; set; } // Необязательный заголовок шага

        [Required]
        public StepType Type { get; set; } // Тип (Text, Quiz и т.д.)

        public int Order { get; set; }

        // --- ПОЛЯ КОНТЕНТА ---

        // Для типа Text: храним HTML разметку
        public string? TextContent { get; set; }

        // Для типа Video: ссылка (YouTube/Vimeo)
        public string? VideoUrl { get; set; }

        // Для типа Code: шаблон кода и проверочный скрипт
        public string? CodeTemplate { get; set; }
        public string? ExpectedOutput { get; set; }

        // Внешний ключ на урок
        public int LessonId { get; set; }
        [ForeignKey("LessonId")]
        public LessonModel Lesson { get; set; }

        // Связь: Один шаг (Quiz) -> Много вариантов ответа
        public List<QuizOptionModel> QuizOptions { get; set; } = new();
    }

    // 6. ВАРИАНТ ОТВЕТА (Для тестов)
    public class QuizOptionModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public bool IsCorrect { get; set; } // Является ли ответ верным

        // Внешний ключ на шаг
        public int StepId { get; set; }
        [ForeignKey("StepId")]
        public StepModel Step { get; set; }
    }

    // 7. ПРОГРЕСС (Для отслеживания прохождения курса студентом)
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