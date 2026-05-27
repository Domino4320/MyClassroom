using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using System.Diagnostics;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDBContext _db;
        private readonly CertificatePdfService _certificatePdfService;

        public CourseController(ApplicationDBContext db, CertificatePdfService certificatePdfService)
        {
            _db = db;
            _certificatePdfService = certificatePdfService;
        }

        private async Task<(bool IsEnrolled, bool HasCompletedCourse)> GetCourseCompletionStatusAsync(int courseId, string login)
        {
            var isEnrolled = await _db.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.UserLogin == login);

            if (!isEnrolled)
                return (false, false);

            var allStepIds = await _db.Steps
                .Where(s => s.Lesson.Module.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync();

            if (!allStepIds.Any())
                return (true, false);

            var completedCount = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted && allStepIds.Contains(p.StepId))
                .Select(p => p.StepId)
                .Distinct()
                .CountAsync();

            return (true, completedCount >= allStepIds.Count);
        }

        // --- СТРАНИЦА ОПИСАНИЯ КУРСА (DETAILS) ---
        public async Task<IActionResult> Details(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Author)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            // --- РАСЧЕТ АНАЛИТИКИ ---
            double recPercent = 0;
            if (course.Reviews != null && course.Reviews.Any())
            {
                int totalReviews = course.Reviews.Count;
                int recommendedCount = course.Reviews.Count(r => r.IsRecommended);
                recPercent = (double)recommendedCount / totalReviews * 100;
            }

            var allInCat = await _db.Courses
                .Where(c => c.Category == course.Category && c.IsPublished)
                .Include(c => c.Reviews)
                .ToListAsync();

            // Если текущего курса нет в списке опубликованных (например, смотрит автор), добавляем для расчета ранга
            if (!allInCat.Any(c => c.Id == id))
            {
                allInCat.Add(course);
            }

            var rankedList = allInCat
                .OrderByDescending(c => c.Reviews != null && c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0)
                .ToList();

            int categoryRank = rankedList.FindIndex(c => c.Id == id) + 1;

            ViewData["RecPercent"] = Math.Round(recPercent, 0);
            ViewData["CategoryRank"] = categoryRank == 0 ? "-" : categoryRank.ToString();
            ViewData["TotalInCat"] = allInCat.Count(c => c.IsPublished);

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
                    var completedCount = await _db.Progress
                        .Where(p => p.UserLogin == login && p.IsCompleted && allStepIds.Contains(p.StepId))
                        .Select(p => p.StepId)
                        .Distinct()
                        .CountAsync();

                    hasCompletedCourse = completedCount >= allStepIds.Count;
                }

                // --- БАЛЛЫ ЗА ТЕСТЫ В КУРСЕ ---
                if (isEnrolled)
                {
                    var maxPointsInCourse = await _db.Steps
                        .Where(s => s.Lesson.Module.CourseId == id && s.Type == StepType.Quiz)
                        .SumAsync(s => (int?)s.MaxPoints) ?? 0;

                    var earnedPointsInCourse = await _db.StepSubmissions
                        .Where(s => s.UserLogin == login &&
                                    !s.IsPending &&
                                    s.Step.Lesson.Module.CourseId == id &&
                                    s.Step.Type == StepType.Quiz)
                        .SumAsync(s => (int?)s.EarnedPoints) ?? 0;

                    ViewData["CourseEarnedPoints"] = earnedPointsInCourse;
                    ViewData["CourseMaxPoints"] = maxPointsInCourse;
                }
            }

            ViewData["IsEnrolled"] = isEnrolled;
            ViewData["HasCompletedCourse"] = hasCompletedCourse;

            double teacherAvgRating = 0;
            int teacherReviewsCount = 0;
            if (!string.IsNullOrEmpty(course.AuthorLogin))
            {
                var teacherRatings = await _db.TeacherReviews
                    .AsNoTracking()
                    .Where(r => r.TeacherLogin == course.AuthorLogin)
                    .Select(r => r.Rating)
                    .ToListAsync();
                teacherReviewsCount = teacherRatings.Count;
                if (teacherRatings.Count > 0)
                    teacherAvgRating = Math.Round(teacherRatings.Average(), 1);
            }

            ViewData["TeacherAvgRating"] = teacherAvgRating;
            ViewData["TeacherReviewsCount"] = teacherReviewsCount;

            return View(course);
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

            // После записи отправляем на первый урок
            return RedirectToAction("Index", new { courseId = courseId });
        }

        [HttpGet]
        public async Task<IActionResult> PreviewCertificate(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return RedirectToAction("Index", "Authorization");

            var (isEnrolled, hasCompletedCourse) = await GetCourseCompletionStatusAsync(courseId, login);
            if (!isEnrolled || !hasCompletedCourse)
                return Forbid();

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (course == null || user == null)
                return NotFound();

            var userDisplayName = string.IsNullOrWhiteSpace(user.Username) ? user.Login : user.Username;
            var pdf = _certificatePdfService.GenerateCourseCertificate(userDisplayName, course.Title, DateTime.UtcNow);
            Response.Headers.ContentDisposition = $"inline; filename=\"certificate-course-{courseId}.pdf\"";
            return File(pdf, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCertificate(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return RedirectToAction("Index", "Authorization");

            var (isEnrolled, hasCompletedCourse) = await GetCourseCompletionStatusAsync(courseId, login);
            if (!isEnrolled || !hasCompletedCourse)
                return Forbid();

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (course == null || user == null)
                return NotFound();

            var userDisplayName = string.IsNullOrWhiteSpace(user.Username) ? user.Login : user.Username;
            var pdf = _certificatePdfService.GenerateCourseCertificate(userDisplayName, course.Title, DateTime.UtcNow);
            var fileName = $"certificate-course-{courseId}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        // --- СТРАНИЦА ОБУЧЕНИЯ (ПЛЕЕР) ---
        public async Task<IActionResult> Index(int courseId, int? stepId = null)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            // Проверка, записан ли пользователь
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

            if (!allStepsSorted.Any()) return Content("В курсе еще нет контента.");

            var completedStepIds = await _db.Progress
                .Where(p => p.UserLogin == login && p.IsCompleted)
                .Select(p => p.StepId)
                .ToListAsync();

            StepModel currentStep;
            if (stepId == null)
            {
                // Находим первый непройденный шаг или берем самый первый
                currentStep = allStepsSorted.FirstOrDefault(s => !completedStepIds.Contains(s.Id)) ?? allStepsSorted.First();
            }
            else
            {
                currentStep = allStepsSorted.FirstOrDefault(s => s.Id == stepId);
            }

            if (currentStep == null) return NotFound();

            var courseBookmark = await _db.CourseBookmarks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserLogin == login && b.CourseId == courseId);

            var isBookmarkStep = courseBookmark != null && courseBookmark.StepId == currentStep.Id;

            // Проверка последовательности (нельзя прыгнуть вперед, кроме сохранённой закладки)
            int currentIndex = allStepsSorted.IndexOf(currentStep);
            if (currentIndex > 0 && !isBookmarkStep)
            {
                var prevStep = allStepsSorted[currentIndex - 1];
                if (!completedStepIds.Contains(prevStep.Id))
                {
                    var lastAvailable = allStepsSorted.FirstOrDefault(s => !completedStepIds.Contains(s.Id)) ?? allStepsSorted.First();
                    return RedirectToAction("Index", new { courseId = courseId, stepId = lastAvailable.Id });
                }
            }

            // Загружаем полные данные шага (квизы, комменты)
            var stepWithDetails = await _db.Steps
                .Include(s => s.QuizOptions)
                .Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == currentStep.Id);

            var comments = await _db.Comments
                .Where(c => c.StepId == currentStep.Id)
                .Include(c => c.User)
                .Include(c => c.ParentComment)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var latestSubmission = await _db.StepSubmissions
                .Where(s => s.StepId == currentStep.Id && s.UserLogin == login)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new StepSubmissionViewModel
                {
                    IsPending = s.IsPending,
                    EarnedPoints = s.EarnedPoints,
                    MaxPoints = s.Step.MaxPoints,
                    TeacherComment = s.TeacherComment,
                    SubmittedAt = s.SubmittedAt
                })
                .FirstOrDefaultAsync();

            var hasPendingManual = await _db.StepSubmissions.AnyAsync(s =>
                s.StepId == currentStep.Id &&
                s.UserLogin == login &&
                s.IsPending &&
                s.Step.IsManualCheck);

            var course = await _db.Courses.FindAsync(courseId);

            // Баллы по курсу (только тесты)
            var maxPointsInCourse = await _db.Steps
                .Where(s => s.Lesson.Module.CourseId == courseId && s.Type == StepType.Quiz)
                .SumAsync(s => (int?)s.MaxPoints) ?? 0;

            var earnedPointsInCourse = await _db.StepSubmissions
                .Where(s => s.UserLogin == login &&
                            !s.IsPending &&
                            s.Step.Lesson.Module.CourseId == courseId &&
                            s.Step.Type == StepType.Quiz)
                .SumAsync(s => (int?)s.EarnedPoints) ?? 0;

            var viewModel = new CourseLearnViewModel
            {
                CourseId = courseId,
                CurrentStep = stepWithDetails ?? currentStep,
                AllModules = modules,
                CompletedStepIds = completedStepIds,
                Comments = comments,
                IsAuthor = course?.AuthorLogin == login,
                IsLastStep = currentIndex == allStepsSorted.Count - 1,
                HasPendingManualSubmission = hasPendingManual,
                LatestSubmission = latestSubmission,
                EarnedPointsInCourse = earnedPointsInCourse,
                MaxPointsInCourse = maxPointsInCourse,
                BookmarkedStepId = courseBookmark?.StepId,
                IsCurrentStepBookmarked = isBookmarkStep
            };

            return View(viewModel);
        }

        // --- ЗАВЕРШЕНИЕ ШАГА (AJAX) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteStep(int stepId, [FromForm] List<int> selectedOptionIds, [FromForm] string? manualAnswer)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return Json(new { success = false, message = "Сессия истекла" });

            var currentStep = await _db.Steps
                .Include(s => s.QuizOptions)
                .Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);

            if (currentStep == null) return Json(new { success = false, message = "Шаг не найден" });

            // Логика проверки теста
            if (currentStep.Type == StepType.Quiz)
            {
                if (currentStep.IsManualCheck)
                {
                    if (string.IsNullOrWhiteSpace(manualAnswer))
                        return Json(new { success = false, message = "Введите текст ответа!" });

                    // Если уже есть активная работа на проверке — не создаем дубликат
                    var hasPending = await _db.StepSubmissions.AnyAsync(s =>
                        s.StepId == stepId &&
                        s.UserLogin == login &&
                        s.IsPending);

                    if (!hasPending)
                    {
                        // Сохраняем ответ на проверку
                        var submission = new StepSubmissionModel
                        {
                            StepId = stepId,
                            UserLogin = login,
                            UserAnswerText = manualAnswer,
                            IsPending = true,
                            SubmittedAt = DateTime.UtcNow
                        };
                        _db.StepSubmissions.Add(submission);
                        await _db.SaveChangesAsync();
                    }

                    // ВАЖНО: ручная проверка НЕ засчитывает шаг, пока преподаватель не выставит баллы
                    return Json(new { success = true, pendingReview = true });
                }
                else if (!string.IsNullOrWhiteSpace(currentStep.CorrectTextAnswer))
                {
                    if (string.IsNullOrWhiteSpace(manualAnswer))
                        return Json(new { success = false, message = "Введите текст ответа!" });

                    static string Normalize(string s) => (s ?? "")
                        .Trim()
                        .ToLowerInvariant()
                        .Replace("\r\n", "\n")
                        .Replace("\r", "\n");

                    var expected = Normalize(currentStep.CorrectTextAnswer);
                    var actual = Normalize(manualAnswer);

                    if (!string.Equals(expected, actual, StringComparison.Ordinal))
                        return Json(new { success = false, message = "Неверный ответ!" });
                }
                else
                {
                    // Автоматическая проверка
                    var correctIds = currentStep.QuizOptions.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(id => id).ToList();
                    var userIds = (selectedOptionIds ?? new List<int>()).OrderBy(id => id).ToList();

                    if (!correctIds.SequenceEqual(userIds))
                    {
                        return Json(new { success = false, message = "Неверный ответ!" });
                    }
                }

                // Автоматические тесты (и по тексту, и по вариантам) должны давать баллы.
                // Создаем запись попытки/результата, чтобы можно было суммировать баллы по курсу.
                if (!currentStep.IsManualCheck)
                {
                    var hasResult = await _db.StepSubmissions.AnyAsync(s =>
                        s.StepId == stepId &&
                        s.UserLogin == login &&
                        !s.IsPending);

                    if (!hasResult)
                    {
                        _db.StepSubmissions.Add(new StepSubmissionModel
                        {
                            StepId = stepId,
                            UserLogin = login,
                            UserAnswerText = manualAnswer,
                            IsPending = false,
                            IsCorrect = true,
                            EarnedPoints = Math.Max(0, currentStep.MaxPoints),
                            SubmittedAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();
                    }
                }
            }

            // Обновляем прогресс
            var progress = await _db.Progress.FirstOrDefaultAsync(p => p.StepId == stepId && p.UserLogin == login);
            if (progress == null)
            {
                _db.Progress.Add(new UserProgressModel
                {
                    StepId = stepId,
                    UserLogin = login,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            // Ищем следующий ID для перехода
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

        // --- ДОБАВЛЕНИЕ ОТЗЫВА (AJAX) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int courseId, int rating, string text, bool isRecommended)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false, message = "Нужно войти в систему" });

            // Проверка: пройден ли курс полностью
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
                return Json(new { success = false, message = "Для отзыва нужно пройти все уроки курса." });
            }

            if (await _db.CourseReviews.AnyAsync(r => r.CourseId == courseId && r.UserLogin == login))
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

        // --- ДОБАВЛЕНИЕ КОММЕНТАРИЯ (AJAX) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int stepId, string text, int? parentCommentId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login) || string.IsNullOrWhiteSpace(text))
                return Json(new { success = false });

            var step = await _db.Steps
                .Include(s => s.Lesson)
                    .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);

            if (step?.Lesson?.Module == null)
                return Json(new { success = false });

            var courseId = step.Lesson.Module.CourseId;
            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            var isTeacherReply = course != null && course.AuthorLogin == login;

            if (parentCommentId.HasValue)
            {
                var parent = await _db.Comments
                    .AsNoTracking()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value && c.StepId == stepId);
                if (parent == null) return Json(new { success = false });

                if (!string.Equals(parent.UserLogin, login, StringComparison.OrdinalIgnoreCase))
                {
                    var authorName = isTeacherReply ? "Преподаватель" : (HttpContext.Session.GetString("Username") ?? login);
                    var courseTitle = course?.Title ?? "курс";
                    _db.Notifications.Add(new NotificationModel
                    {
                        UserLogin = parent.UserLogin,
                        Title = isTeacherReply ? "Ответ преподавателя" : "Ответ на ваш комментарий",
                        Body = $"{authorName} ответил(а) вам в курсе «{courseTitle}».",
                        Url = Url.Action("Index", "Course", new { courseId, stepId })
                    });
                }
            }

            _db.Comments.Add(new CommentModel
            {
                StepId = stepId,
                UserLogin = login,
                Text = text,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // --- РЕДАКТИРОВАНИЕ ОСНОВНОЙ ИНФОРМАЦИИ (GET) ---
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

        // --- РЕДАКТИРОВАНИЕ ОСНОВНОЙ ИНФОРМАЦИИ (POST) ---
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

                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads")))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads"));

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