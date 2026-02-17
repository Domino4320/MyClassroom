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
            var myCourses = await (from course in _context.Courses
                                   join user in _context.Users on course.AuthorLogin equals user.Login
                                   where course.AuthorLogin == currentUserLogin // Фильтр по сессии
                                   select new CourseCardModel
                                   {
                                       Id = course.Id,
                                       Title = course.Title,
                                       Description = course.Description,
                                       Category = course.Category,
                                       CoverImagePath = course.CoverImagePath,
                                       CreatedAt = course.CreatedAt,
                                       AuthorUsername = user.Username, // Имя из таблицы Users
                                       AuthorAvatar = user.Avatar      // Аватар из таблицы Users
                                   }).ToListAsync();

            return View(myCourses);
        }
    }
}
