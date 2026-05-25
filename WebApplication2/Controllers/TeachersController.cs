using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class TeachersController : Controller
    {
        private readonly ApplicationDBContext _db;

        public TeachersController(ApplicationDBContext db)
        {
            _db = db;
        }

        private static async Task<bool> UserHasCompletedAnyPublishedCourseByTeacherAsync(
            ApplicationDBContext db, string userLogin, string teacherLogin)
        {
            var courseIds = await db.Courses
                .AsNoTracking()
                .Where(c => c.AuthorLogin == teacherLogin && c.IsPublished)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var courseId in courseIds)
            {
                var enrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserLogin == userLogin);
                if (!enrolled)
                    continue;

                var allStepIds = await db.Steps
                    .Where(s => s.Lesson.Module.CourseId == courseId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (allStepIds.Count == 0)
                    continue;

                var completedCount = await db.Progress
                    .Where(p => p.UserLogin == userLogin && p.IsCompleted && allStepIds.Contains(p.StepId))
                    .Select(p => p.StepId)
                    .Distinct()
                    .CountAsync();

                if (completedCount >= allStepIds.Count)
                    return true;
            }

            return false;
        }

        public async Task<IActionResult> Index()
        {
            var rows = await (from u in _db.Users
                              join tp in _db.TeacherProfiles on u.Login equals tp.UserLogin
                              where u.Role == "Teacher"
                              select new { tp, u }).ToListAsync();

            var logins = rows.Select(r => r.tp.UserLogin).ToList();

            var reviewStats = await _db.TeacherReviews
                .Where(r => logins.Contains(r.TeacherLogin))
                .GroupBy(r => r.TeacherLogin)
                .Select(g => new { TeacherLogin = g.Key, Avg = g.Average(x => (double)x.Rating), Count = g.Count() })
                .ToListAsync();

            var reviewDict = reviewStats.ToDictionary(x => x.TeacherLogin, x => (x.Avg, x.Count));

            var courseCounts = await _db.Courses
                .Where(c => c.IsPublished && logins.Contains(c.AuthorLogin!))
                .GroupBy(c => c.AuthorLogin!)
                .Select(g => new { Author = g.Key, Count = g.Count() })
                .ToListAsync();

            var countDict = courseCounts.ToDictionary(x => x.Author, x => x.Count);

            var vm = rows.Select(r =>
            {
                reviewDict.TryGetValue(r.tp.UserLogin, out var rv);
                countDict.TryGetValue(r.tp.UserLogin, out var cc);
                var about = r.tp.About ?? "";
                var snippet = about.Length > 140 ? about[..140].TrimEnd() + "…" : about;

                return new TeacherCatalogRowViewModel
                {
                    Login = r.tp.UserLogin,
                    Username = r.u.Username,
                    Avatar = r.u.Avatar,
                    CurrentJob = r.tp.CurrentJob,
                    SpecializationCategory = r.tp.SpecializationCategory ?? "",
                    TeacherTags = r.tp.TeacherTags,
                    Experience = r.tp.Experience,
                    AboutSnippet = string.IsNullOrWhiteSpace(snippet) ? null : snippet,
                    PublishedCoursesCount = cc,
                    AverageRating = rv.Avg,
                    ReviewsCount = rv.Count
                };
            })
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.ReviewsCount)
                .ThenBy(t => t.Username)
                .ToList();

            return View(vm);
        }

        public async Task<IActionResult> PublicProfile(string id)
        {
            var login = id;
            if (string.IsNullOrWhiteSpace(login))
                return NotFound();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login);
            if (user == null || user.Role != "Teacher")
                return NotFound();

            var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(tp => tp.UserLogin == login);
            if (profile == null)
                return NotFound();

            var reviews = await _db.TeacherReviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.TeacherLogin == login)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var sessionLogin = HttpContext.Session.GetString("Login");
            var hasAlready = !string.IsNullOrEmpty(sessionLogin) &&
                             reviews.Any(r => r.UserLogin == sessionLogin);

            var canLeave = !string.IsNullOrEmpty(sessionLogin) &&
                           sessionLogin != login &&
                           !hasAlready &&
                           await UserHasCompletedAnyPublishedCourseByTeacherAsync(_db, sessionLogin!, login);

            var publishedCourses = await _db.Courses
                .AsNoTracking()
                .Where(c => c.AuthorLogin == login && c.IsPublished)
                .OrderBy(c => c.Title)
                .Select(c => new TeacherCourseLinkViewModel { Id = c.Id, Title = c.Title })
                .ToListAsync();

            var publishedCount = publishedCourses.Count;
            var avg = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;

            var displayItems = reviews.Select(r => new TeacherReviewDisplayItem
            {
                AuthorLogin = r.UserLogin,
                AuthorDisplayName = string.IsNullOrWhiteSpace(r.User?.Username) ? r.UserLogin : r.User!.Username,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = r.CreatedAt,
                IsRecommended = r.IsRecommended
            }).ToList();

            var vm = new TeacherPublicProfileViewModel
            {
                Login = login,
                Username = user.Username,
                Avatar = user.Avatar,
                Profile = profile,
                Reviews = displayItems,
                AverageRating = avg,
                ReviewsCount = reviews.Count,
                PublishedCoursesCount = publishedCount,
                CanLeaveReview = canLeave,
                HasAlreadyReviewed = hasAlready,
                IsOwnProfile = sessionLogin == login,
                PublishedCourses = publishedCourses
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(string teacherLogin, int rating, string text, bool isRecommended)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            if (string.IsNullOrWhiteSpace(teacherLogin) || teacherLogin == login)
                return Json(new { success = false, message = "Нельзя оставить отзыв самому себе." });

            var teacherUser = await _db.Users.FirstOrDefaultAsync(u => u.Login == teacherLogin && u.Role == "Teacher");
            if (teacherUser == null || !await _db.TeacherProfiles.AnyAsync(tp => tp.UserLogin == teacherLogin))
                return Json(new { success = false, message = "Преподаватель не найден." });

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(text))
                return Json(new { success = false, message = "Укажите оценку (1–5) и текст отзыва." });

            if (text.Length > 1000)
                return Json(new { success = false, message = "Текст отзыва не длиннее 1000 символов." });

            if (!await UserHasCompletedAnyPublishedCourseByTeacherAsync(_db, login, teacherLogin))
                return Json(new { success = false, message = "Отзыв можно оставить после прохождения хотя бы одного курса этого преподавателя." });

            if (await _db.TeacherReviews.AnyAsync(r => r.TeacherLogin == teacherLogin && r.UserLogin == login))
                return Json(new { success = false, message = "Вы уже оставили отзыв этому преподавателю." });

            _db.TeacherReviews.Add(new TeacherReviewModel
            {
                TeacherLogin = teacherLogin,
                UserLogin = login,
                Rating = rating,
                Text = text.Trim(),
                IsRecommended = isRecommended,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
