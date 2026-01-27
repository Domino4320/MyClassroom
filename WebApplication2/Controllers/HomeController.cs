using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index()
        {
            var courses = new List<CourseCardModel>
        {
            new CourseCardModel
            {
                CourseName = "Введение в Frontend",
                CourseTeacherName = "Алексей Петров",
                CourseTeacherAvatar = "https://i.pravatar.cc/100?img=5",
                CourseStudentsAmount = 18
            },
            new CourseCardModel
            {
                CourseName = "Введение в Backend",
                CourseTeacherName = "Иван Федоров",
                CourseTeacherAvatar = "https://i.pravatar.cc/100?img=6",
                CourseStudentsAmount = 90
            }
        };

            return View(courses);
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
        // Task - потому что асинхронный подход
        public async Task<IActionResult> UploadAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
            {
                return Json(new { error = "Файл не выбран" });
            }
            var login = HttpContext.Session.GetString("Login");
            if (login == null || login == "") {
                return Json(new { error = "Пользователь не найден" });
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
                return Json(new { error = "Пользователь не найден" });
            }
            return Json(new { avatar = "/avatars/" + filename });
        }
    }
}
