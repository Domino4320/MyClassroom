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

        public async Task<IActionResult> Index()
        {
            // ВАЖНО: Берем логин именно из Session, как это делает твой Sidebar
            var userLogin = HttpContext.Session.GetString("Login");

            // Если в сессии пусто — значит контроллер тебя "не видит" как авторизованного
            if (string.IsNullOrEmpty(userLogin))
            {
                // Пока просто возвращаем на главную, чтобы не было 404
                return RedirectToAction("Index", "Home");
            }

            // Получаем курсы, привязанные к этому логину через прогресс
            var activeCourses = await _context.Courses
                .Where(c => _context.UserProgress
                    .Any(p => p.UserLogin == userLogin && p.Step.Lesson.Module.CourseId == c.Id))
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath,
                    CreatedAt = c.CreatedAt,
                    AuthorUsername = c.AuthorLogin ?? "Автор",
                    AuthorAvatar = "/images/default_avatar.jpg"
                })
                .ToListAsync();

            return View(activeCourses);
        }
    }
}