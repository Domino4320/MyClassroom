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
            // 1. Достаем логин из сессии
            string currentUserLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(currentUserLogin))
            {
                return RedirectToAction("Index", "Home");
            }

            // 2. Выполняем запрос с JOIN и фильтрацией WHERE
            // ... внутри метода Index ...

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

                                       // ВОТ ЭТОЙ СТРОКИ НЕ ХВАТАЛО:
                                       IsPublished = course.IsPublished
                                   }).ToListAsync();

            return View(myCourses);
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

            // РАСЧЕТ ПРОЦЕНТА РЕКОМЕНДАЦИЙ
            double recPercent = 0;
            if (course.Reviews != null && course.Reviews.Any())
            {
                // Считаем только те отзывы, где стоит галочка IsRecommended
                int totalReviews = course.Reviews.Count;
                int recommendedCount = course.Reviews.Count(r => r.IsRecommended);
                recPercent = (double)recommendedCount / totalReviews * 100;
            }

            // РАСЧЕТ МЕСТА В КАТЕГОРИИ
            var categoryRank = 0;
            var allInCat = await _context.Courses
                .Where(c => c.Category == course.Category)
                .Include(c => c.Reviews)
                .ToListAsync();

            var rankedList = allInCat
                .OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0)
                .ToList();

            categoryRank = rankedList.FindIndex(c => c.Id == id) + 1;

            // Передаем данные через ViewData, чтобы не менять тип модели на странице
            ViewData["RecPercent"] = Math.Round(recPercent, 0);
            ViewData["CategoryRank"] = categoryRank;
            ViewData["TotalInCat"] = allInCat.Count;

            // ... остальная логика (IsEnrolled и т.д.)
            return View(course);
        }
    }
}
