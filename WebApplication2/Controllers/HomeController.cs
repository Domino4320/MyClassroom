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
            // 1. Извлекаем логин пользователя из сессии
            var userLogin = HttpContext.Session.GetString("Login");

            // 2. Если пользователь НЕ авторизован — ставим флаг гостя и прерываем выполнение
            if (string.IsNullOrEmpty(userLogin))
            {
                ViewBag.IsGuest = true;
                return View();
            }

            // Пользователь авторизован
            ViewBag.IsGuest = false;

            // 3. Получаем список ID курсов, на которые пользователь уже записан (чтобы не предлагать их)
            var enrolledData = await _db.Enrollments
                .Where(e => e.UserLogin == userLogin)
                .Select(e => new { e.CourseId, e.Course.Category })
                .ToListAsync();

            var enrolledIds = enrolledData.Select(x => x.CourseId).ToList();
            var userCategories = enrolledData.Select(x => x.Category).Distinct().ToList();

            // 4. ОПРЕДЕЛЯЕМ БАЗОВЫЙ ЗАПРОС (Base Query)
            // Условия: Опубликован + Не куплен + Не мой (я не автор)
            var baseQuery = _db.Courses
                .Where(c => c.IsPublished)
                .Where(c => !enrolledIds.Contains(c.Id))
                .Where(c => c.AuthorLogin != userLogin)
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath,
                    // Подтягиваем данные автора напрямую из таблицы пользователей
                    AuthorUsername = _db.Users
                        .Where(u => u.Login == c.AuthorLogin)
                        .Select(u => u.Username)
                        .FirstOrDefault() ?? "Автор",
                    AuthorAvatar = _db.Users
                        .Where(u => u.Login == c.AuthorLogin)
                        .Select(u => u.Avatar)
                        .FirstOrDefault() ?? "/images/default_avatar.jpg",
                    // Считаем рейтинг и процент рекомендаций
                    AverageRating = c.Reviews.Any() ? Math.Round(c.Reviews.Average(r => r.Rating), 1) : 0,
                    RecPercent = c.Reviews.Any()
                        ? (int)((double)c.Reviews.Count(r => r.Rating >= 4) / c.Reviews.Count() * 100)
                        : 0
                });

            // 5. ФОРМИРУЕМ СПИСКИ ДЛЯ ВЬЮ (по 8 курсов максимум)

            // Блок 1: Продолжайте обучение (по категориям пользователя)
            var categoryRecs = new List<CourseCardModel>();
            if (userCategories.Any())
            {
                categoryRecs = await baseQuery
                    .Where(c => userCategories.Contains(c.Category))
                    .OrderByDescending(c => c.AverageRating)
                    .Take(8)
                    .ToListAsync();
            }

            // Блок 2: Высшая оценка (Рейтинг от 4.0)
            var topRated = await baseQuery
                .Where(c => c.AverageRating >= 4.0)
                .OrderByDescending(c => c.AverageRating)
                .Take(8)
                .ToListAsync();

            // Блок 3: Хиты платформы (Сортировка по количеству записей в Enrollments)
            // Здесь мы делаем отдельную выборку, так как нужно считать данные из другой таблицы
            var popular = await _db.Courses
                .Where(c => c.IsPublished && !enrolledIds.Contains(c.Id) && c.AuthorLogin != userLogin)
                .OrderByDescending(c => _db.Enrollments.Count(e => e.CourseId == c.Id))
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath,
                    AuthorUsername = _db.Users.Where(u => u.Login == c.AuthorLogin).Select(u => u.Username).FirstOrDefault() ?? "Автор",
                    AuthorAvatar = _db.Users.Where(u => u.Login == c.AuthorLogin).Select(u => u.Avatar).FirstOrDefault() ?? "/images/default_avatar.jpg",
                    AverageRating = c.Reviews.Any() ? Math.Round(c.Reviews.Average(r => r.Rating), 1) : 0,
                    RecPercent = c.Reviews.Any() ? (int)((double)c.Reviews.Count(r => r.Rating >= 4) / c.Reviews.Count() * 100) : 0
                })
                .Take(8)
                .ToListAsync();

            // Блок 4: Студенты рекомендуют (Высокий процент рекомендаций)
            var highRec = await baseQuery
                .Where(c => c.RecPercent >= 70)
                .OrderByDescending(c => c.RecPercent)
                .Take(8)
                .ToListAsync();

            // Передаем всё во View через ViewBag
            ViewBag.CategoryRecs = categoryRecs;
            ViewBag.TopRated = topRated;
            ViewBag.Popular = popular;
            ViewBag.HighRec = highRec;

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
            {
                return Json(new { error = "Р¤Р°Р№Р» РЅРµ РІС‹Р±СЂР°РЅ" });
            }

            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
            {
                return Json(new { error = "РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ РІ СЃРµСЃСЃРёРё" });
            }

            // 1. РЎРѕС…СЂР°РЅРµРЅРёРµ С„Р°Р№Р»Р°
            var filename = $"{Guid.NewGuid()}{Path.GetExtension(Avatar.FileName)}";
            var directory = Path.Combine(_wwwroot.WebRootPath, "avatars");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, filename);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Avatar.CopyToAsync(stream);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user != null)
            {
                string newAvatarPath = "/avatars/" + filename;
                user.Avatar = newAvatarPath;

                await _db.SaveChangesAsync();
                HttpContext.Session.SetString("Avatar", newAvatarPath);
                await HttpContext.Session.CommitAsync();

                return Json(new { avatar = newAvatarPath });
            }

            return Json(new { error = "РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ РІ Р±Р°Р·Рµ РґР°РЅРЅС‹С…" });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}