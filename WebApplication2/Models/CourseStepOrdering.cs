namespace WebApplication2.Models
{
    public static class CourseStepOrdering
    {
        public static IEnumerable<StepModel> SortSteps(IEnumerable<StepModel> steps) =>
            steps.OrderBy(s => s.Order).ThenBy(s => s.Id);

        public static List<StepModel> FlattenCourseSteps(IEnumerable<ModuleModel> modules) =>
            modules
                .OrderBy(m => m.Order)
                .SelectMany(m => m.Lessons.OrderBy(l => l.Order))
                .SelectMany(l => SortSteps(l.Steps ?? new List<StepModel>()))
                .ToList();
    }
}
