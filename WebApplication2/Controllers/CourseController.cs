using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using System.Diagnostics;

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
            // ДОБАВЛЕНО: .Include(c => c.Author) для вывода профиля преподавателя
            var course = await _db.Courses
        .Include(c => c.Modules)
            .ThenInclude(m => m.Lessons)
        .Include(c => c.Reviews)
        .Include(c => c.Author) // ВАЖНО: Подгружаем данные преподавателя по логину
        .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            // --- РАСЧЕТ АНАЛИТИКИ ---

            // 1. Процент рекомендаций
            double recPercent = 0;
            if (course.Reviews != null && course.Reviews.Any())
            {
                int totalReviews = course.Reviews.Count;
                int recommendedCount = course.Reviews.Count(r => r.IsRecommended);
                recPercent = (double)recommendedCount / totalReviews * 100;
            }

            // 2. Место в категории и общее кол-во курсов в категории (ТОЛЬКО ОПУБЛИКОВАННЫЕ)
            var allInCat = await _db.Courses
                .Where(c => c.Category == course.Category && c.IsPublished) // Добавили фильтр IsPublished
                .Include(c => c.Reviews)
                .ToListAsync();

            // Если текущий курс еще не опубликован, он может не попасть в список allInCat.
            // Чтобы аналитика не ломалась для автора черновика, добавим его в список для расчета, если его там нет
            if (!allInCat.Any(c => c.Id == id))
            {
                allInCat.Add(course);
            }

            var rankedList = allInCat
                .OrderByDescending(c => c.Reviews != null && c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0)
                .ToList();

            int categoryRank = rankedList.FindIndex(c => c.Id == id) + 1;

            // Передаем данные во View
            ViewData["RecPercent"] = Math.Round(recPercent, 0);
            ViewData["CategoryRank"] = categoryRank == 0 ? "-" : categoryRank.ToString();
            ViewData["TotalInCat"] = allInCat.Count(c => c.IsPublished); // Показываем только кол-во публичных курсов

            // --- ПРОВЕРКА ЗАПИСИ И ПРОГРЕССА ---
            var login = HttpContext.Session.GetString("Login");
            bool isEnrolled = false;
            bool hasCompletedCourse = false;

            if (!string.IsNullOrEmpty(login))
            {
                isEnrolled = await _db.Enrollments
                    .AnyAsync(e => e.CourseId == id && e.UserLogin == login);

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

                    hasCompletedCourse = completedStepIds.Count >= allStepIds.Count;
                }
            }

            ViewData["IsEnrolled"] = isEnrolled;
            ViewData["HasCompletedCourse"] = hasCompletedCourse;

            return View(course);
        }

        // --- ДОБАВЛЕНИЕ ОТЗЫВА (AJAX) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int courseId, int rating, string text, bool isRecommended)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            var allStepIds = await _db.Steps
                .Where(s => s.Lesson.Module.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync();

            int completedCount = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted && allStepIds.Contains(p.StepId))
                .Select(p => p.StepId)
                .Distinct()
                .CountAsync();

            if (allStepIds.Count == 0 || completedCount < allStepIds.Count)
            {
                return Json(new { success = false, message = "Для отзыва нужно пройти все уроки." });
            }

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
                IsRecommended = isRecommended,
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

            if (currentStep.Type == StepType.Quiz)
            {
                var correctIds = currentStep.QuizOptions.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(id => id).ToList();
                var userIds = (selectedOptionIds ?? new List<int>()).OrderBy(id => id).ToList();

                if (!correctIds.SequenceEqual(userIds))
                {
                    return Json(new { success = false, message = "Неверный ответ!" });
                }
            }

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

        // --- РЕДАКТИРОВАНИЕ КУРСА (GET) ---
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            if (course.AuthorLogin != login) return Forbid();

            return View(course);
        }

        // --- РЕДАКТИРОВАНИЕ КУРСА (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseModel model, IFormFile? uploadCover)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var courseInDb = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (courseInDb == null) return NotFound();
            if (courseInDb.AuthorLogin != login) return Forbid();

            courseInDb.Title = model.Title;
            courseInDb.Description = model.Description;
            courseInDb.Category = model.Category;
            courseInDb.IsPublished = model.IsPublished;

            if (uploadCover != null && uploadCover.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadCover.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadCover.CopyToAsync(stream);
                }

                courseInDb.CoverImagePath = "/uploads/" + fileName;
            }

            _db.Courses.Update(courseInDb);
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = courseInDb.Id });
        }
    }
}