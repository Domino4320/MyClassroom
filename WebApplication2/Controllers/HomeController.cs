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
            // 1. ��������� ����� ������������ �� ������
            var userLogin = HttpContext.Session.GetString("Login");

            // 2. ���� ������������ �� ����������� � ������ ���� ����� � ��������� ����������
            if (string.IsNullOrEmpty(userLogin))
            {
                ViewBag.IsGuest = true;
                return View();
            }

            // ������������ �����������
            ViewBag.IsGuest = false;

            // 3. �������� ������ ID ������, �� ������� ������������ ��� ������� (����� �� ���������� ��)
            var enrolledData = await _db.Enrollments
                .Where(e => e.UserLogin == userLogin)
                .Select(e => new { e.CourseId, e.Course.Category })
                .ToListAsync();

            var enrolledIds = enrolledData.Select(x => x.CourseId).ToList();
            var userCategories = enrolledData.Select(x => x.Category).Distinct().ToList();

            // 4. ���������� ������� ������ (Base Query)
            // �������: ����������� + �� ������ + �� ��� (� �� �����)
            var baseQuery = _db.Courses
                .Where(c => c.IsPublished)
                .Where(c => !enrolledIds.Contains(c.Id))
                .Where(c => c.AuthorLogin != userLogin)
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description ?? "�������� �����������",
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = c.CreatedAt,
                    IsPublished = c.IsPublished,
                    AuthorUsername = _db.Users
                        .Where(u => u.Login == c.AuthorLogin)
                        .Select(u => u.Username)
                        .FirstOrDefault() ?? "�����",
                    AuthorAvatar = _db.Users
                        .Where(u => u.Login == c.AuthorLogin)
                        .Select(u => u.Avatar)
                        .FirstOrDefault() ?? "/images/default_avatar.jpg",
                    AverageRating = c.Reviews.Any() ? Math.Round(c.Reviews.Average(r => r.Rating), 1) : 0,
                    RecPercent = c.Reviews.Any()
                        ? (int)((double)c.Reviews.Count(r => r.Rating >= 4) / c.Reviews.Count() * 100)
                        : 0
                });

            // 5. ��������� ������ ��� ��� (�� 8 ������ ��������)

            // ���� 1: ����������� �������� (�� ���������� ������������)
            var categoryRecs = new List<CourseCardModel>();
            if (userCategories.Any())
            {
                categoryRecs = await baseQuery
                    .Where(c => userCategories.Contains(c.Category))
                    .OrderByDescending(c => c.AverageRating)
                    .Take(8)
                    .ToListAsync();
            }

            // ���� 2: ������ ������ (������� �� 4.0)
            var topRated = await baseQuery
                .Where(c => c.AverageRating >= 4.0)
                .OrderByDescending(c => c.AverageRating)
                .Take(8)
                .ToListAsync();

            // ���� 3: ���� ��������� (���������� �� ���������� ������� � Enrollments)
            // ����� �� ������ ��������� �������, ��� ��� ����� ������� ������ �� ������ �������
            var popular = await _db.Courses
                .Where(c => c.IsPublished && !enrolledIds.Contains(c.Id) && c.AuthorLogin != userLogin)
                .OrderByDescending(c => _db.Enrollments.Count(e => e.CourseId == c.Id))
                .Select(c => new CourseCardModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description ?? "Описание отсутствует",
                    Category = c.Category,
                    CoverImagePath = c.CoverImagePath ?? "/images/default-course.jpg",
                    CreatedAt = c.CreatedAt,
                    IsPublished = c.IsPublished,
                    AuthorUsername = _db.Users.Where(u => u.Login == c.AuthorLogin).Select(u => u.Username).FirstOrDefault() ?? "Автор",
                    AuthorAvatar = _db.Users.Where(u => u.Login == c.AuthorLogin).Select(u => u.Avatar).FirstOrDefault() ?? "/images/default_avatar.jpg",
                    AverageRating = c.Reviews.Any() ? Math.Round(c.Reviews.Average(r => r.Rating), 1) : 0,
                    RecPercent = c.Reviews.Any() ? (int)((double)c.Reviews.Count(r => r.Rating >= 4) / c.Reviews.Count() * 100) : 0
})
                .Take(8)
                .ToListAsync();

            // ���� 4: �������� ����������� (������� ������� ������������)
            var highRec = await baseQuery
                .Where(c => c.RecPercent >= 70)
                .OrderByDescending(c => c.RecPercent)
                .Take(8)
                .ToListAsync();

            // �������� �� �� View ����� ViewBag
            ViewBag.CategoryRecs = NormalizeCardDates(categoryRecs);
            ViewBag.TopRated = NormalizeCardDates(topRated);
            ViewBag.Popular = NormalizeCardDates(popular);
            ViewBag.HighRec = NormalizeCardDates(highRec);

            return View();
        }

        private static List<CourseCardModel> NormalizeCardDates(List<CourseCardModel> cards)
        {
            foreach (var card in cards)
                card.CreatedAt = CourseDisplayHelper.NormalizeCreatedAt(card.CreatedAt);
            return cards;
        }
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile Avatar)
        {
            if (Avatar == null || Avatar.Length == 0)
            {
                return Json(new { error = "���� �� ������" });
            }

            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
            {
                return Json(new { error = "������������ �� ������ � ������" });
            }

            // 1. ���������� �����
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

            return Json(new { error = "������������ �� ������ � ���� ������" });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}