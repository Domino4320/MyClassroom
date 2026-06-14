using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class ActiveCoursesController : Controller
    {
        private readonly ApplicationDBContext _context;

        public ActiveCoursesController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var userLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(userLogin))
            {
                return RedirectToAction("Index", "Home");
            }

            const int pageSize = CourseListPageViewModel.DefaultPageSize;
            page = Math.Max(1, page);

            var activeQuery = _context.Courses.Where(c =>
                _context.UserProgress.Any(p =>
                    p.UserLogin == userLogin && p.Step.Lesson.Module.CourseId == c.Id));

            var totalCount = await activeQuery.CountAsync();
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            if (totalPages > 0 && page > totalPages)
                return RedirectToAction(nameof(Index), new { page = totalPages });

            var items = await (
                from course in activeQuery
                orderby course.CreatedAt descending
                join user in _context.Users on course.AuthorLogin equals user.Login into userJoin
                from author in userJoin.DefaultIfEmpty()
                select new CourseCardModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Category = course.Category,
                    CoverImagePath = course.CoverImagePath,
                    CreatedAt = course.CreatedAt,
                    AuthorUsername = author != null ? author.Username : (course.AuthorLogin ?? "Автор"),
                    AuthorAvatar = (author != null && !string.IsNullOrEmpty(author.Avatar))
                        ? author.Avatar
                        : "/images/default_avatar.svg",
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

            var categories = await activeQuery
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

            return View(model);
        }
    }
}
