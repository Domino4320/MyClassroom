namespace WebApplication2.Models
{
    public class CourseCardModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public string? CoverImagePath { get; set; }
        public DateTime CreatedAt { get; set; }

        public string AuthorUsername { get; set; }
        public string? AuthorAvatar { get; set; }
    }
}