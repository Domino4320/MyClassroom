using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _wwwroot;
        private readonly ApplicationDBContext _db; 

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, ApplicationDBContext db)
        {
            _logger = logger;
            _wwwroot = env;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {

            // --- БЛОК ДИАГНОСТИКИ ---
            var rawCourses = await _db.Courses.ToListAsync();
            var rawUsers = await _db.Users.ToListAsync();

            Console.WriteLine("=== ПРОВЕРКА ДАННЫХ БД ===");
            Console.WriteLine($"Курсов в базе: {rawCourses.Count}");
            foreach (var c in rawCourses)
            {
                Console.WriteLine($"Курс: {c.Title} | AuthorLogin в базе: '{c.AuthorLogin}'");
            }

            Console.WriteLine($"Пользователей в базе: {rawUsers.Count}");
            foreach (var u in rawUsers)
            {
                Console.WriteLine($"User Login: '{u.Login}' | Username: {u.Username}");
            }
            Console.WriteLine("==========================");
            // --- КОНЕЦ БЛОКА ДИАГНОСТИКИ ---с
            // Соединяем таблицу Courses и Users
            var query = from course in _db.Courses
                        join user in _db.Users on course.AuthorLogin equals user.Login
                        select new CourseCardModel
                        {
                            Id = course.Id,
                            Title = course.Title,
                            Description = course.Description,
                            Category = course.Category,
                            CoverImagePath = course.CoverImagePath,
                            CreatedAt = course.CreatedAt,
                            // Берем данные из найденного пользователя:
                            AuthorUsername = user.Username,
                            AuthorAvatar = user.Avatar
                        };

            var list = await query.ToListAsync();
            return View(list); // Передаем список CourseCardModel в View
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
            {
                return Json(new { error = "���� �� ������" });
            }
            var login = HttpContext.Session.GetString("Login");
            if (login == null || login == "") {
                return Json(new { error = "������������ �� ������" });
            }
            var filename = $"{Guid.NewGuid()}{Path.GetExtension(Avatar.FileName)}";
            var directory = Path.Combine(_wwwroot.WebRootPath, "avatars");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, filename);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Avatar.CopyToAsync(stream);
            }
            var user = _db.Users.FirstOrDefault(u => u.Login == login);
            if (user!=null)
            {
                user.Avatar = "/avatars/" + filename;
                _db.SaveChanges();
            }
            else
            {
                return Json(new { error = "������������ �� ������" });
            }
            return Json(new { avatar = "/avatars/" + filename });
        }
    } 
}
