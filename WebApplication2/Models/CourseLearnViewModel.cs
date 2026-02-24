namespace WebApplication2.Models
{
    public class CourseLearnViewModel
    {
        public int CourseId { get; set; }
        public StepModel CurrentStep { get; set; }
        public List<ModuleModel> AllModules { get; set; }

        // Добавь эти три свойства, на которые ругается ошибка:
        public List<int> CompletedStepIds { get; set; } = new List<int>();
        public List<CommentModel> Comments { get; set; } = new List<CommentModel>();
        public bool IsAuthor { get; set; }
    }
}