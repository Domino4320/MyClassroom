using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class BookmarksController : Controller
    {
        private readonly ApplicationDBContext _db;

        public BookmarksController(ApplicationDBContext db)
        {
            _db = db;
        }

        private string? RequireLogin()
        {
            return HttpContext.Session.GetString("Login");
        }

        private async Task<bool> IsEnrolledAsync(string login, int courseId)
        {
            return await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserLogin == login);
        }

        private async Task<bool> StepBelongsToCourseAsync(int stepId, int courseId)
        {
            return await _db.Steps.AnyAsync(s =>
                s.Id == stepId && s.Lesson.Module.CourseId == courseId);
        }

        public async Task<IActionResult> Index()
        {
            var login = RequireLogin();
            if (string.IsNullOrEmpty(login))
                return RedirectToAction("Index", "Authorization");

            var bookmarks = await _db.CourseBookmarks
                .AsNoTracking()
                .Include(b => b.Course)
                .Include(b => b.Step)
                    .ThenInclude(s => s.Lesson)
                        .ThenInclude(l => l.Module)
                .Where(b => b.UserLogin == login)
                .OrderByDescending(b => b.UpdatedAt)
                .ToListAsync();

            var vm = bookmarks.Select(b => new BookmarkListItemViewModel
            {
                CourseId = b.CourseId,
                CourseTitle = b.Course?.Title ?? "Курс",
                CoverImagePath = b.Course?.CoverImagePath,
                StepId = b.StepId,
                StepTitle = string.IsNullOrWhiteSpace(b.Step?.Title) ? $"Шаг #{b.StepId}" : b.Step!.Title!,
                LessonTitle = b.Step?.Lesson?.Title ?? "",
                ModuleTitle = b.Step?.Lesson?.Module?.Title ?? "",
                UpdatedAt = b.UpdatedAt
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(int courseId, int stepId)
        {
            var login = RequireLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            if (!await IsEnrolledAsync(login, courseId))
                return Json(new { success = false, message = "Вы не записаны на этот курс." });

            if (!await StepBelongsToCourseAsync(stepId, courseId))
                return Json(new { success = false, message = "Шаг не найден в этом курсе." });

            var bookmark = await _db.CourseBookmarks
                .FirstOrDefaultAsync(b => b.UserLogin == login && b.CourseId == courseId);

            if (bookmark == null)
            {
                _db.CourseBookmarks.Add(new CourseBookmarkModel
                {
                    UserLogin = login,
                    CourseId = courseId,
                    StepId = stepId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                bookmark.StepId = stepId;
                bookmark.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, stepId, message = "Закладка сохранена" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int courseId)
        {
            var login = RequireLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            var bookmark = await _db.CourseBookmarks
                .FirstOrDefaultAsync(b => b.UserLogin == login && b.CourseId == courseId);

            if (bookmark == null)
                return Json(new { success = true, message = "Закладки не было" });

            _db.CourseBookmarks.Remove(bookmark);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Закладка удалена" });
        }
    }
}
