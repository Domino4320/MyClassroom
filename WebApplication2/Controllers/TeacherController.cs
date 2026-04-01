using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using Microsoft.AspNetCore.Authorization;
using WebApplication2.Data;

namespace WebApplication2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDBContext _context;

        public TeacherController(ApplicationDBContext context)
        {
            _context = context;
        }

        // Страница со списком всех курсов автора, где есть непроверенные работы
        public async Task<IActionResult> GradingCenter()
        {
            var userLogin = User.Identity.Name;

            // Находим все курсы этого автора
            var courses = await _context.Courses
                .Where(c => c.AuthorLogin == userLogin)
                .Select(c => new
                {
                    CourseId = c.Id,
                    Title = c.Title,
                    PendingCount = _context.StepSubmissions
                        .Count(s => s.Step.Lesson.Module.CourseId == c.Id && s.IsPending)
                })
                .ToListAsync();

            return View(courses);
        }

        // Страница проверки конкретного курса
        public async Task<IActionResult> GradeCourse(int id)
        {
            var userLogin = User.Identity.Name;

            var pendingSubmissions = await _context.StepSubmissions
                .Include(s => s.Step)
                .ThenInclude(st => st.Lesson)
                .Where(s => s.Step.Lesson.Module.CourseId == id && s.IsPending)
                .OrderBy(s => s.SubmittedAt)
                .Select(s => new GradingViewModel
                {
                    SubmissionId = s.Id,
                    StudentLogin = s.UserLogin,
                    StepTitle = s.Step.Title,
                    LessonTitle = s.Step.Lesson.Title,
                    UserAnswer = s.UserAnswerText,
                    ReferenceAnswer = s.Step.CorrectTextAnswer,
                    MaxPoints = s.Step.MaxPoints,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();

            ViewBag.CourseTitle = _context.Courses.Find(id)?.Title;
            return View(pendingSubmissions);
        }

        // API метод для сохранения оценки
        [HttpPost]
        public async Task<IActionResult> SubmitGrade([FromBody] SubmitGradeDto data)
        {
            var submission = await _context.StepSubmissions
                .Include(s => s.Step)
                .FirstOrDefaultAsync(s => s.Id == data.SubmissionId);

            if (submission == null) return NotFound();

            submission.EarnedPoints = data.Grade;
            submission.TeacherComment = data.Comment;
            submission.IsPending = false;
            submission.IsCorrect = data.Grade > 0; // Считаем пройденным, если баллов > 0

            // Также обновляем общий прогресс пользователя, чтобы шаг зачелся
            var progress = await _context.UserProgress
                .FirstOrDefaultAsync(p => p.UserLogin == submission.UserLogin && p.StepId == submission.StepId);

            if (progress == null)
            {
                _context.UserProgress.Add(new UserProgressModel
                {
                    UserLogin = submission.UserLogin,
                    StepId = submission.StepId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                progress.IsCompleted = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    // Вспомогательные DTO
    public class GradingViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentLogin { get; set; }
        public string StepTitle { get; set; }
        public string LessonTitle { get; set; }
        public string UserAnswer { get; set; }
        public string ReferenceAnswer { get; set; }
        public int MaxPoints { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class SubmitGradeDto
    {
        public int SubmissionId { get; set; }
        public int Grade { get; set; }
        public string Comment { get; set; }
    }
}