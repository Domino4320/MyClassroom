namespace WebApplication2.Models
{
    public class CourseAnalyticsViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }

        // Для графика (Labels и Данные)
        public List<string> Dates { get; set; } = new();
        public List<int> EnrollmentsPerDay { get; set; } = new();

        // Статистика
        public int TotalStudents { get; set; }
        public double AverageRating { get; set; }
        public double RecommendationRate { get; set; }

        // Рейтинг в категории
        public int RankInCategory { get; set; }
        public int TotalInSameCategory { get; set; }
    }
}