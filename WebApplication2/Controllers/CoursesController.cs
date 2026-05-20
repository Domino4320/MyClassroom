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
            // Соединяем курсы с пользователями по логину
            var query = from course in _context.Courses
                        where course.IsPublished
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

                            // Берем Username из UserModel, если нашли, иначе AuthorLogin
                            AuthorUsername = author != null ? author.Username : (course.AuthorLogin ?? "Аноним"),

                            // Берем Avatar из UserModel (поле называется Avatar, а не AvatarPath)
                            AuthorAvatar = (author != null && !string.IsNullOrEmpty(author.Avatar))
                                           ? author.Avatar
                                           : "/images/default_avatar.jpg",

                            AverageRating = course.Reviews.Any()
                                ? Math.Round(course.Reviews.Average(r => r.Rating), 1)
                                : 0
                        };

            var model = await query.ToListAsync();

            ViewData["Title"] = "Библиотека курсов";
            return View(model);
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