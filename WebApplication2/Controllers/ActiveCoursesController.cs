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

            var items = await activeQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath,
                    CreatedAt = c.CreatedAt,
                    AuthorUsername = c.AuthorLogin ?? "Автор",
                    AuthorAvatar = "/images/default_avatar.jpg",
                    AverageRating = c.Reviews.Any()
                        ? Math.Round(c.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    RecPercent = c.Reviews.Any()
                        ? CourseDisplayHelper.GetRecommendationPercent(
                            c.Reviews.Count,
                            c.Reviews.Count(r => r.IsRecommended))
                        : 0
                })
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
