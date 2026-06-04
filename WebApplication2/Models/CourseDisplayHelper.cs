namespace WebApplication2.Models
{
    public static class CourseDisplayHelper
    {
        private static readonly DateTime MinValidCreatedAt = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime NormalizeCreatedAt(DateTime createdAt)
        {
            if (createdAt >= MinValidCreatedAt)
                return createdAt;

            return DateTime.UtcNow;
        }

        public static bool HasValidCreatedAt(DateTime createdAt) => createdAt >= MinValidCreatedAt;

        public static int GetRecommendationPercent(int reviewCount, int recommendedCount)
        {
            if (reviewCount <= 0) return 0;
            return (int)Math.Round(100.0 * recommendedCount / reviewCount, MidpointRounding.AwayFromZero);
        }
    }
}
