namespace WebApplication2.Models
{
    public class CourseCardModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = "/images/default_avatar.jpg";

        // Обязательные поля для связи с твоей CourseModel
        public DateTime CreatedAt { get; set; }
        public bool IsPublished { get; set; }
        public double AverageRating { get; set; }
        public int RecPercent { get; set; }
    }
}