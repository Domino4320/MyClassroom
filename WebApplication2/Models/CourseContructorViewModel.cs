namespace WebApplication2.Models
{
    public class CourseConstructorViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public List<ModuleModel> Modules { get; set; }
    }
}
