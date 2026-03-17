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

        public async Task<IActionResult> Index()
        {
            string currentUserLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(currentUserLogin))
            {
                return RedirectToAction("Index", "Home");
            }

            var myCourses = await (from course in _context.Courses
                                   join user in _context.Users on course.AuthorLogin equals user.Login
                                   where course.AuthorLogin == currentUserLogin
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
                                       IsPublished = course.IsPublished
                                   }).ToListAsync();

            return View(myCourses);
        }

        // --- НОВЫЙ МЕТОД УДАЛЕНИЯ ---
        [HttpPost]
        [Route("MyCourses/DeleteCourse/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            string currentUserLogin = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(currentUserLogin)) return Unauthorized();

            // Загружаем курс со всей иерархией
            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound("Курс не найден");
            if (course.AuthorLogin != currentUserLogin) return Forbid();

            try
            {
                // 1. Удаляем отзывы (используем то имя таблицы, которое у вас в контексте)
                // Если таблица называется CourseReviews, оставляем так:
                var reviews = _context.CourseReviews.Where(r => r.CourseId == id);
                if (reviews.Any()) _context.CourseReviews.RemoveRange(reviews);

                // 2. Удаляем курс. 
                // Благодаря Include(...) выше, EF поймет связи и удалит модули/уроки/шаги, 
                // если в базе настроено каскадное удаление.
                _context.Courses.Remove(course);

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                // Если падает из-за внешних ключей, значит нужно удалять шаги вручную:
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

            // Расчет процентов
            double recPercent = 0;
            if (course.Reviews != null && course.Reviews.Any())
            {
                int totalReviews = course.Reviews.Count;
                int recommendedCount = course.Reviews.Count(r => r.IsRecommended);
                recPercent = (double)recommendedCount / totalReviews * 100;
            }

            // Расчет рейтинга в категории
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