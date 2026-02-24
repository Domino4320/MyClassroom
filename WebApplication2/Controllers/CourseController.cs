using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDBContext _db;

        public CourseController(ApplicationDBContext db)
        {
            _db = db;
        }

        // --- ДОБАВЛЕНО: Страница деталей курса (Витрина) ---
        public async Task<IActionResult> Details(int id)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            var login = HttpContext.Session.GetString("Login");

            // Проверяем, записан ли уже пользователь, чтобы поменять кнопку на "Продолжить"
            bool isEnrolled = false;
            if (!string.IsNullOrEmpty(login))
            {
                isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.UserLogin == login);
            }

            ViewData["IsEnrolled"] = isEnrolled;
            return View(course);
        }

        // --- ДОБАВЛЕНО: Метод записи на курс (Enroll) ---
        // Исправляет ошибку 404 при нажатии кнопки "Поступить"
        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            // Проверяем, нет ли уже записи
            var existingEnrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserLogin == login);

            if (existingEnrollment == null)
            {
                var enrollment = new EnrollmentModel
                {
                    CourseId = courseId,
                    UserLogin = login,
                    EnrolledAt = DateTime.Now
                };
                _db.Enrollments.Add(enrollment);
                await _db.SaveChangesAsync();
            }

            // После успешной записи отправляем в плеер обучения
            return RedirectToAction("Index", new { courseId = courseId });
        }

        // --- Твой существующий метод Learn ---
        public async Task<IActionResult> Index(int courseId, int? stepId = null)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserLogin == login);

            // Если не записан, отправляем на страницу деталей (витрину)
            if (!isEnrolled) return RedirectToAction("Details", new { id = courseId });

            var modules = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Steps)
                .OrderBy(m => m.Order)
                .ToListAsync();

            StepModel currentStep;
            if (stepId == null)
            {
                currentStep = modules.SelectMany(m => m.Lessons).SelectMany(l => l.Steps).OrderBy(s => s.Order).FirstOrDefault();
            }
            else
            {
                currentStep = await _db.Steps
                    .Include(s => s.QuizOptions)
                    .FirstOrDefaultAsync(s => s.Id == stepId);
            }

            if (currentStep == null) return Content("Контент не найден.");

            var completedStepIds = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted)
                .Select(p => p.StepId)
                .ToListAsync();

            var comments = await _db.Comments
                .Where(c => c.StepId == currentStep.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var course = await _db.Courses.FindAsync(courseId);
            bool isAuthor = course?.AuthorLogin == login;

            var viewModel = new CourseLearnViewModel
            {
                CourseId = courseId,
                CurrentStep = currentStep,
                AllModules = modules,
                CompletedStepIds = completedStepIds,
                Comments = comments,
                IsAuthor = isAuthor
            };

            // В методе Learn вместо return View(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteStep(int stepId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return Json(new { success = false });

            var progress = await _db.Progress
                .FirstOrDefaultAsync(p => p.StepId == stepId && p.UserLogin == login);

            if (progress == null)
            {
                progress = new UserProgressModel
                {
                    StepId = stepId,
                    UserLogin = login,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };
                _db.Progress.Add(progress);
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int stepId, string text)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login) || string.IsNullOrWhiteSpace(text)) return Json(new { success = false });

            var comment = new CommentModel
            {
                StepId = stepId,
                UserLogin = login,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return Json(new { success = true, user = login, date = comment.CreatedAt.ToString("g") });
        }
    }
}