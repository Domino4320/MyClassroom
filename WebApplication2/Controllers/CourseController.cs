using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using System.Diagnostics; // Для отладки в Output

namespace WebApplication2.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDBContext _db;

        public CourseController(ApplicationDBContext db)
        {
            _db = db;
        }

        // --- СТРАНИЦА ОПИСАНИЯ КУРСА (DETAILS) ---
        public async Task<IActionResult> Details(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var login = HttpContext.Session.GetString("Login");
            bool isEnrolled = false;
            bool hasCompletedCourse = false;

            if (!string.IsNullOrEmpty(login))
            {
                // 1. Проверяем запись
                isEnrolled = await _db.Enrollments
                    .AnyAsync(e => e.CourseId == id && e.UserLogin == login);

                // 2. Улучшенная проверка прогресса
                // Получаем ID всех шагов, которые принадлежат этому курсу
                // 2. Улучшенная проверка прогресса
                // Запрашиваем ID шагов напрямую из базы, а не из навигационных свойств
                var allStepIds = await _db.Steps
                    .Where(s => s.Lesson.Module.CourseId == id)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (allStepIds.Any())
                {
                    var completedStepIds = await _db.Progress
                        .Where(p => p.UserLogin == login && p.IsCompleted && allStepIds.Contains(p.StepId))
                        .Select(p => p.StepId)
                        .Distinct()
                        .ToListAsync();

                    // ВАЖНО: используем >= на случай, если в базе есть дубликаты прогресса
                    hasCompletedCourse = completedStepIds.Count >= allStepIds.Count;

                    Debug.WriteLine($"[DEBUG] User: {login}, Course: {id}, Total Steps: {allStepIds.Count}, Completed: {completedStepIds.Count}");
                }
                else
                {
                    // Если в курсе 0 шагов, технически он не пройден
                    hasCompletedCourse = false;
                }
            }

            ViewData["IsEnrolled"] = isEnrolled;
            ViewData["HasCompletedCourse"] = hasCompletedCourse;

            return View(course);
        }

        // --- ДОБАВЛЕНИЕ ОТЗЫВА (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> AddReview(int courseId, int rating, string text)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            // 1. Получаем список всех шагов курса из БД напрямую
            var allStepIds = await _db.Steps
                .Where(s => s.Lesson.Module.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync();

            // 2. Считаем, сколько из них пройдено
            int completedCount = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted && allStepIds.Contains(p.StepId))
                .Select(p => p.StepId)
                .Distinct()
                .CountAsync();

            if (allStepIds.Count == 0 || completedCount < allStepIds.Count)
            {
                return Json(new
                {
                    success = false,
                    message = $"Для отзыва нужно пройти все уроки. Пройдено: {completedCount} из {allStepIds.Count}"
                });
            }

            // 3. Проверка на дубликат отзыва
            var alreadyExists = await _db.CourseReviews
                .AnyAsync(r => r.CourseId == courseId && r.UserLogin == login);

            if (alreadyExists)
                return Json(new { success = false, message = "Вы уже оставили отзыв." });

            var review = new CourseReviewModel
            {
                CourseId = courseId,
                UserLogin = login,
                Rating = rating,
                Text = text,
                CreatedAt = DateTime.Now
            };

            _db.CourseReviews.Add(review);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // --- ЗАПИСЬ НА КУРС ---
        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var alreadyEnrolled = await _db.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.UserLogin == login);

            if (!alreadyEnrolled)
            {
                _db.Enrollments.Add(new EnrollmentModel
                {
                    CourseId = courseId,
                    UserLogin = login,
                    EnrolledAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { courseId = courseId });
        }

        // --- СТРАНИЦА ОБУЧЕНИЯ (ПЛЕЕР) ---
        public async Task<IActionResult> Index(int courseId, int? stepId = null)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var isEnrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserLogin == login);
            if (!isEnrolled) return RedirectToAction("Details", new { id = courseId });

            var modules = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Steps)
                .OrderBy(m => m.Order)
                .ToListAsync();

            var allStepsSorted = modules
                .SelectMany(m => m.Lessons.OrderBy(l => l.Order)
                    .SelectMany(l => l.Steps.OrderBy(s => s.Order)))
                .ToList();

            if (!allStepsSorted.Any()) return Content("В курсе еще нет шагов.");

            var completedStepIds = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted)
                .Select(p => p.StepId)
                .ToListAsync();

            StepModel currentStep;
            if (stepId == null)
            {
                currentStep = allStepsSorted.FirstOrDefault(s => !completedStepIds.Contains(s.Id)) ?? allStepsSorted.First();
            }
            else
            {
                currentStep = allStepsSorted.FirstOrDefault(s => s.Id == stepId);
            }

            if (currentStep == null) return NotFound();

            // Защита от прыжков через уроки
            int currentIndex = allStepsSorted.IndexOf(currentStep);
            if (currentIndex > 0)
            {
                var prevStep = allStepsSorted[currentIndex - 1];
                if (!completedStepIds.Contains(prevStep.Id))
                {
                    var lastAvailable = allStepsSorted.FirstOrDefault(s => !completedStepIds.Contains(s.Id)) ?? allStepsSorted.First();
                    return RedirectToAction("Index", new { courseId = courseId, stepId = lastAvailable.Id });
                }
            }

            var stepWithDetails = await _db.Steps
                .Include(s => s.QuizOptions)
                .Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == currentStep.Id);

            var comments = await _db.Comments
                .Where(c => c.StepId == currentStep.Id)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var course = await _db.Courses.FindAsync(courseId);

            var viewModel = new CourseLearnViewModel
            {
                CourseId = courseId,
                CurrentStep = stepWithDetails ?? currentStep,
                AllModules = modules,
                CompletedStepIds = completedStepIds,
                Comments = comments,
                IsAuthor = course?.AuthorLogin == login,
                IsLastStep = currentIndex == allStepsSorted.Count - 1
            };

            return View(viewModel);
        }

        // --- ЗАВЕРШЕНИЕ ШАГА ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteStep(int stepId, [FromForm] List<int> selectedOptionIds)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return Json(new { success = false, message = "Сессия истекла" });

            var currentStep = await _db.Steps
                .Include(s => s.QuizOptions)
                .Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);

            if (currentStep == null) return Json(new { success = false, message = "Шаг не найден" });

            // Проверка квиза
            if (currentStep.Type == StepType.Quiz)
            {
                var correctIds = currentStep.QuizOptions.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(id => id).ToList();
                var userIds = (selectedOptionIds ?? new List<int>()).OrderBy(id => id).ToList();

                if (!correctIds.SequenceEqual(userIds))
                {
                    return Json(new { success = false, message = "Неверный ответ!" });
                }
            }

            // Сохранение прогресса
            var progress = await _db.Progress.FirstOrDefaultAsync(p => p.StepId == stepId && p.UserLogin == login);
            if (progress == null)
            {
                _db.Progress.Add(new UserProgressModel { StepId = stepId, UserLogin = login, IsCompleted = true });
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            // Поиск следующего шага
            var allIds = await _db.Modules
                .Where(m => m.CourseId == currentStep.Lesson.Module.CourseId)
                .OrderBy(m => m.Order)
                .SelectMany(m => m.Lessons.OrderBy(l => l.Order)
                    .SelectMany(l => l.Steps.OrderBy(s => s.Order)))
                .Select(s => s.Id)
                .ToListAsync();

            int idx = allIds.IndexOf(stepId);
            int? nextId = (idx >= 0 && idx < allIds.Count - 1) ? allIds[idx + 1] : null;

            return Json(new { success = true, nextStepId = nextId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int stepId, string text)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login) || string.IsNullOrWhiteSpace(text))
                return Json(new { success = false });

            _db.Comments.Add(new CommentModel
            {
                StepId = stepId,
                UserLogin = login,
                Text = text,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}