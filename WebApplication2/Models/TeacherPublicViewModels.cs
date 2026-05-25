using System;
using System.Collections.Generic;

namespace WebApplication2.Models
{
    public class TeacherCatalogRowViewModel
    {
        public string Login { get; set; }
        public string Username { get; set; }
        public string? Avatar { get; set; }
        public string CurrentJob { get; set; }
        public string SpecializationCategory { get; set; } = string.Empty;
        public string? TeacherTags { get; set; }
        public int Experience { get; set; }
        public string? AboutSnippet { get; set; }
        public int PublishedCoursesCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
    }

    public class TeacherReviewDisplayItem
    {
        public string AuthorDisplayName { get; set; }
        public string AuthorLogin { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class TeacherPublicProfileViewModel
    {
        public string Login { get; set; }
        public string Username { get; set; }
        public string? Avatar { get; set; }
        public TeacherProfile Profile { get; set; }
        public IReadOnlyList<TeacherReviewDisplayItem> Reviews { get; set; } = Array.Empty<TeacherReviewDisplayItem>();
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public int PublishedCoursesCount { get; set; }
        public bool CanLeaveReview { get; set; }
        public bool HasAlreadyReviewed { get; set; }
        public bool IsOwnProfile { get; set; }
        public IReadOnlyList<TeacherCourseLinkViewModel> PublishedCourses { get; set; } = Array.Empty<TeacherCourseLinkViewModel>();
    }

    public class TeacherCourseLinkViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}
