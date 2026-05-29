namespace WebApplication2.Models
{
    public class CourseConstructorViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }

        // Добавьте это свойство, чтобы ушла ошибка CS0117
        public bool IsPublished { get; set; }

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

        public int EarnedPointsInCourse { get; set; }
        public int MaxPointsInCourse { get; set; }

        // Статус ручной проверки по текущему шагу
        public bool HasPendingManualSubmission { get; set; }
        public StepSubmissionViewModel? LatestSubmission { get; set; }

        public int? BookmarkedStepId { get; set; }
        public bool IsCurrentStepBookmarked { get; set; }

        public List<CourseTimelineStepViewModel> TimelineSteps { get; set; } = new();
        public int TimelineCompletedCount { get; set; }
        public int TimelineTotalCount { get; set; }
    }

    public class CourseTimelineStepViewModel
    {
        public int StepId { get; set; }
        public string Title { get; set; } = "";
        public StepType Type { get; set; }
        public string ModuleTitle { get; set; } = "";
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsLocked { get; set; }
    }

    public class StepSubmissionViewModel
    {
        public bool IsPending { get; set; }
        public int EarnedPoints { get; set; }
        public int MaxPoints { get; set; }
        public string? TeacherComment { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class LessonUpdateDto
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public List<StepUpdateDto> Steps { get; set; }
    }

    public class StepUpdateDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? TextContent { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsMultipleChoice { get; set; }
        public bool IsManualCheck { get; set; }
        public string? CorrectTextAnswer { get; set; }
        public int MaxPoints { get; set; }
        public string? CodeTemplate { get; set; }
        public string? ExpectedOutput { get; set; }
    }
}