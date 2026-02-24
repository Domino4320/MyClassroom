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
            var query = from course in _db.Courses
                        where course.IsPublished == true
                        join user in _db.Users on course.AuthorLogin equals user.Login
                        select new CourseCardModel
                        {
                            Id = course.Id,
                            Title = course.Title,
                            Description = course.Description,
                            Category = course.Category,
                            CoverImagePath = string.IsNullOrEmpty(course.CoverImagePath)
                                ? "/images/default-course.png"
                                : (course.CoverImagePath.StartsWith("/") ? course.CoverImagePath : "/" + course.CoverImagePath),
                            CreatedAt = course.CreatedAt,
                            AuthorUsername = user.Username,
                            // Исправляем: гарантируем наличие слеша в начале
                            AuthorAvatar = string.IsNullOrEmpty(user.Avatar)
                                ? "/images/default_avatar.jpg"
                                : (user.Avatar.StartsWith("/") ? user.Avatar : "/" + user.Avatar)
                        };

            var list = await query.ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
            {
                return Json(new { error = "Файл не выбран" });
            }

            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
            {
                return Json(new { error = "Пользователь не найден в сессии" });
            }

            // 1. Сохранение файла
            var filename = $"{Guid.NewGuid()}{Path.GetExtension(Avatar.FileName)}";
            var directory = Path.Combine(_wwwroot.WebRootPath, "avatars");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, filename);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Avatar.CopyToAsync(stream);
            }

            // 2. Обновление БД
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user != null)
            {
                string newAvatarPath = "/avatars/" + filename;
                user.Avatar = newAvatarPath;

                await _db.SaveChangesAsync();

                // 3. ОБНОВЛЕНИЕ СЕССИИ + ПРИНУДИТЕЛЬНЫЙ COMMIT
                HttpContext.Session.SetString("Avatar", newAvatarPath);
                // Это гарантирует, что сессия запишется в хранилище ДО того, как вернется JSON ответ
                await HttpContext.Session.CommitAsync();

                return Json(new { avatar = newAvatarPath });
            }

            return Json(new { error = "Пользователь не найден в базе данных" });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}