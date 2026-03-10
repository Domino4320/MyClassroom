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

        // 1. Страница "Библиотека курсов" (Общий список)
        public async Task<IActionResult> Index()
        {
            // Берем только опубликованные курсы для общей библиотеки
            var courses = await _context.Courses
                .Where(c => c.IsPublished)
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description ?? "Описание отсутствует",
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = c.CreatedAt,
                    IsPublished = c.IsPublished, // Теперь передаем это свойство

                    // Мапим AuthorLogin из БД в AuthorUsername модели представления
                    AuthorUsername = c.AuthorLogin ?? "Аноним",
                    AuthorAvatar = "/images/default_avatar.jpg"
                })
                .ToListAsync();

            ViewData["Title"] = "Библиотека курсов";
            return View(courses);
        }

        // 2. Страница "Мои курсы" (Личные курсы автора)
        public async Task<IActionResult> MyCourses()
        {
            var userLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(userLogin))
            {
                return RedirectToAction("Login", "Account");
            }

            // Здесь мы показываем ВСЕ свои курсы (и черновики, и опубликованные)
            var myCourses = await _context.Courses
                .Where(c => c.AuthorLogin == userLogin)
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description ?? "Описание отсутствует",
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = c.CreatedAt,
                    IsPublished = c.IsPublished, // Обязательно передаем для скрытия/показа кнопок

                    AuthorUsername = c.AuthorLogin ?? userLogin,
                    AuthorAvatar = "/images/default_avatar.jpg"
                })
                .ToListAsync();

            // Флаг IsEditable нужен, чтобы в представлении появились кнопки управления
            ViewData["IsEditable"] = true;
            ViewData["Title"] = "Мои курсы";

            return View("Index", myCourses);
        }
    }
}