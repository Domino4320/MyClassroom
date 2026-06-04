using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class MyCoursesController : Controller
    {
        private readonly ApplicationDBContext _context;

        public MyCoursesController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            string currentUserLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(currentUserLogin))
            {
                return RedirectToAction("Index", "Home");
            }

            const int pageSize = CourseListPageViewModel.DefaultPageSize;
            page = Math.Max(1, page);

            var myQuery = _context.Courses.Where(c => c.AuthorLogin == currentUserLogin);
            var totalCount = await myQuery.CountAsync();
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            if (totalPages > 0 && page > totalPages)
                return RedirectToAction(nameof(Index), new { page = totalPages });

            var items = await (
                from course in myQuery
                orderby course.CreatedAt descending
                join user in _context.Users on course.AuthorLogin equals user.Login
                select new CourseCardModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Category = course.Category,
                    CoverImagePath = course.CoverImagePath,
                    CreatedAt = course.CreatedAt,
                    AuthorUsername = user.Username,
                    AuthorAvatar = user.Avatar,
                    IsPublished = course.IsPublished,
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

            return View(model);
        }

        [HttpPost]
        [Route("MyCourses/DeleteCourse/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            string currentUserLogin = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(currentUserLogin)) return Unauthorized();

            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound("Курс не найден");
            if (course.AuthorLogin != currentUserLogin) return Forbid();

            try
            {
                var reviews = _context.CourseReviews.Where(r => r.CourseId == id);
                if (reviews.Any()) _context.CourseReviews.RemoveRange(reviews);

                _context.Courses.Remove(course);

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Ошибка базы данных: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Reviews)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            double recPercent = 0;
            if (course.Reviews != null && course.Reviews.Any())
            {
                int totalReviews = course.Reviews.Count;
                int recommendedCount = course.Reviews.Count(r => r.IsRecommended);
                recPercent = (double)recommendedCount / totalReviews * 100;
            }

            var allInCat = await _context.Courses
                .Where(c => c.Category == course.Category)
                .Include(c => c.Reviews)
                .ToListAsync();

            var rankedList = allInCat
                .OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0)
                .ToList();

            ViewData["RecPercent"] = Math.Round(recPercent, 0);
            ViewData["CategoryRank"] = rankedList.FindIndex(c => c.Id == id) + 1;
            ViewData["TotalInCat"] = allInCat.Count;

            return View(course);
        }
    }
}
