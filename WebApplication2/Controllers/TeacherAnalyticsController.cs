using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

namespace WebApplication2.Controllers
{
    public class TeacherAnalyticsController : Controller
    {
        private readonly ApplicationDBContext _db;

        public TeacherAnalyticsController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> CourseStats(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Unauthorized();

            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();
            if (!string.Equals(course.AuthorLogin, login, StringComparison.OrdinalIgnoreCase)) return Forbid();

            var enrolledUserLogins = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e => e.UserLogin)
                .Distinct()
                .ToListAsync();

            var studentsCount = enrolledUserLogins.Count;

            var totalSteps = await _db.Steps
                .AsNoTracking()
                .CountAsync(s => s.Lesson.Module.CourseId == courseId);

            var completedRows = await _db.Progress
                .AsNoTracking()
                .Where(p => p.IsCompleted && enrolledUserLogins.Contains(p.UserLogin) && p.Step.Lesson.Module.CourseId == courseId)
                .Select(p => new { p.UserLogin, p.StepId })
                .Distinct()
                .ToListAsync();

            var completedByUser = completedRows
                .GroupBy(x => x.UserLogin)
                .ToDictionary(g => g.Key, g => g.Count());

            double avgCompletedSteps = studentsCount == 0 ? 0 : completedByUser.Values.DefaultIfEmpty(0).Average();
            double avgCompletedPercent = totalSteps == 0 ? 0 : (avgCompletedSteps / totalSteps) * 100.0;

            // Быстрая “визуальная” метрика: сколько студентов в группах прогресса
            int bucket0 = 0, bucket1 = 0, bucket2 = 0, bucket3 = 0, bucket4 = 0;
            foreach (var u in enrolledUserLogins)
            {
                completedByUser.TryGetValue(u, out var done);
                var pct = totalSteps == 0 ? 0 : (double)done / totalSteps;
                if (pct <= 0.0001) bucket0++;
                else if (pct < 0.25) bucket1++;
                else if (pct < 0.50) bucket2++;
                else if (pct < 0.75) bucket3++;
                else bucket4++;
            }

            return Json(new
            {
                courseId,
                courseTitle = course.Title,
                studentsCount,
                totalSteps,
                avgCompletedSteps = Math.Round(avgCompletedSteps, 1),
                avgCompletedPercent = Math.Round(avgCompletedPercent, 0),
                buckets = new
                {
                    none = bucket0,
                    lt25 = bucket1,
                    lt50 = bucket2,
                    lt75 = bucket3,
                    gte75 = bucket4
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> LessonFeedbackStats(int courseId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login))
                return Unauthorized();

            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();
            if (!string.Equals(course.AuthorLogin, login, StringComparison.OrdinalIgnoreCase)) return Forbid();

            var lessons = await _db.Lessons
                .AsNoTracking()
                .Where(l => l.Module.CourseId == courseId)
                .OrderBy(l => l.Module.Order)
                .ThenBy(l => l.Order)
                .Select(l => new { l.Id, l.Title, ModuleTitle = l.Module.Title })
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.Id).ToList();

            var feedbackRows = await _db.LessonFeedbacks
                .AsNoTracking()
                .Where(f => lessonIds.Contains(f.LessonId))
                .GroupBy(f => f.LessonId)
                .Select(g => new
                {
                    LessonId = g.Key,
                    Count = g.Count(),
                    AvgDifficulty = g.Average(x => x.Difficulty),
                    AvgClarity = g.Average(x => x.Clarity),
                    AvgInterest = g.Average(x => x.Interest)
                })
                .ToListAsync();

            var byLesson = feedbackRows.ToDictionary(x => x.LessonId);

            return Json(new
            {
                courseId,
                courseTitle = course.Title,
                lessons = lessons.Select(l =>
                {
                    byLesson.TryGetValue(l.Id, out var stats);
                    return new
                    {
                        lessonId = l.Id,
                        lessonTitle = l.Title,
                        moduleTitle = l.ModuleTitle,
                        responses = stats?.Count ?? 0,
                        avgDifficulty = stats == null ? (double?)null : Math.Round(stats.AvgDifficulty, 1),
                        avgClarity = stats == null ? (double?)null : Math.Round(stats.AvgClarity, 1),
                        avgInterest = stats == null ? (double?)null : Math.Round(stats.AvgInterest, 1)
                    };
                })
            });
        }
    }
}

