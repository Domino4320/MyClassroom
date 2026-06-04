using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDBContext _context;

        public CoursesController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = CourseListPageViewModel.DefaultPageSize;
            page = Math.Max(1, page);

            var published = _context.Courses.Where(c => c.IsPublished);

            var totalCount = await published.CountAsync();
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            if (totalPages > 0 && page > totalPages)
                return RedirectToAction(nameof(Index), new { page = totalPages });

            var items = await (
                from course in published
                orderby course.CreatedAt descending
                join user in _context.Users on course.AuthorLogin equals user.Login into userJoin
                from author in userJoin.DefaultIfEmpty()
                select new CourseCardModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description ?? "Описание отсутствует",
                    Category = course.Category,
                    CoverImagePath = course.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = course.CreatedAt,
                    IsPublished = course.IsPublished,
                    AuthorUsername = author != null ? author.Username : (course.AuthorLogin ?? "Аноним"),
                    AuthorAvatar = (author != null && !string.IsNullOrEmpty(author.Avatar))
                        ? author.Avatar
                        : "/images/default_avatar.jpg",
                    AverageRating = course.Reviews.Any()
                        ? Math.Round(course.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    RecPercent = course.Reviews.Any()
                        ? CourseDisplayHelper.GetRecommendationPercent(
                            course.Reviews.Count,
                            course.Reviews.Count(r => r.IsRecommended))
                        : 0
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var card in items)
                card.CreatedAt = CourseDisplayHelper.NormalizeCreatedAt(card.CreatedAt);

            var categories = await published
                .Select(c => c.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var model = new CourseListPageViewModel
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Categories = categories
            };

            ViewData["Title"] = "MyClassroom";
            return View(model);
        }

        public async Task<IActionResult> MyCourses(int page = 1)
        {
            var userLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(userLogin))
            {
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = CourseListPageViewModel.DefaultPageSize;
            page = Math.Max(1, page);

            var myQuery = _context.Courses.Where(c => c.AuthorLogin == userLogin);
            var totalCount = await myQuery.CountAsync();
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            if (totalPages > 0 && page > totalPages)
                return RedirectToAction(nameof(MyCourses), new { page = totalPages });

            var items = await (
                from course in myQuery
                orderby course.CreatedAt descending
                join user in _context.Users on course.AuthorLogin equals user.Login
                select new CourseCardModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description ?? "Описание отсутствует",
                    Category = course.Category,
                    CoverImagePath = course.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = course.CreatedAt,
                    IsPublished = course.IsPublished,
                    AuthorUsername = user.Username,
                    AuthorAvatar = user.Avatar,
                    AverageRating = course.Reviews.Any()
                        ? Math.Round(course.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    RecPercent = course.Reviews.Any()
                        ? CourseDisplayHelper.GetRecommendationPercent(
                            course.Reviews.Count,
                            course.Reviews.Count(r => r.IsRecommended))
                        : 0
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var card in items)
                card.CreatedAt = CourseDisplayHelper.NormalizeCreatedAt(card.CreatedAt);

            var model = new CourseListPageViewModel
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            ViewData["IsEditable"] = true;
            ViewData["Title"] = "MyClassroom";
            return View("Index", model);
        }
    }
}
