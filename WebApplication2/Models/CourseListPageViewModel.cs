namespace WebApplication2.Models
{
    public class CourseListPageViewModel
    {
        public const int DefaultPageSize = 20;

        public List<CourseCardModel> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 && TotalCount > 0
            ? (int)Math.Ceiling(TotalCount / (double)PageSize)
            : 0;

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        public List<string> Categories { get; set; } = new();
    }
}
