namespace WebApplication2.Models
{
    public class BookmarkListItemViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
        public int StepId { get; set; }
        public string StepTitle { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public string ModuleTitle { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
