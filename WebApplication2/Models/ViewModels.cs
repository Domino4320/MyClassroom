namespace WebApplication2.Models
{
    public class CourseConstructorViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public List<ModuleModel> Modules { get; set; } = new();
    }

    public class CourseLearnViewModel
    {
        public int CourseId { get; set; }
        public StepModel CurrentStep { get; set; }
        public List<ModuleModel> AllModules { get; set; }
        public List<int> CompletedStepIds { get; set; }
        public bool IsLastStep { get; set; }
        public bool IsAuthor { get; set; }
        public List<CommentModel> Comments { get; set; }
        public string AuthorLogin => CurrentStep?.Lesson?.Module?.Course?.AuthorLogin ?? "";
    }

    public class LessonUpdateDto
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public List<StepUpdateDto> Steps { get; set; } = new();
    }

    public class StepUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? TextContent { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsMultipleChoice { get; set; }
    }
}