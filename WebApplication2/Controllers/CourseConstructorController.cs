using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class CourseConstructorController : Controller
    {
        private readonly ApplicationDBContext _db;

        public CourseConstructorController(ApplicationDBContext db)
        {
            _db = db;
        }

        // 1. Главная страница конструктора
        public async Task<IActionResult> Index(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var userLogin = HttpContext.Session.GetString("Login");
            if (course.AuthorLogin != userLogin) return Forbid();

            var viewModel = new CourseConstructorViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Modules = course.Modules.OrderBy(m => m.Order).ToList()
            };

            return View(viewModel);
        }

        // 2. Добавление Модуля
        [HttpPost]
        public async Task<IActionResult> AddModule(int courseId, string title)
        {
            if (string.IsNullOrEmpty(title)) return BadRequest("Название не может быть пустым");

            var orders = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .Select(m => m.Order)
                .ToListAsync();

            int nextOrder = orders.Any() ? orders.Max() + 1 : 1;

            var newModule = new ModuleModel
            {
                CourseId = courseId,
                Title = title,
                Order = nextOrder
            };

            _db.Modules.Add(newModule);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // 3. Добавление Урока
        [HttpPost]
        public async Task<IActionResult> AddLesson(int moduleId, string title)
        {
            if (string.IsNullOrEmpty(title)) return BadRequest("Название не может быть пустым");

            var orders = await _db.Lessons
                .Where(l => l.ModuleId == moduleId)
                .Select(l => l.Order)
                .ToListAsync();

            int nextOrder = orders.Any() ? orders.Max() + 1 : 1;

            var newLesson = new LessonModel
            {
                ModuleId = moduleId,
                Title = title,
                Order = nextOrder
            };

            _db.Lessons.Add(newLesson);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // 4. Добавление Шага (Лекция, Видео, Тест)
        [HttpPost]
        public async Task<IActionResult> AddStep(int lessonId, string type)
        {
            // Маппинг строкового типа из JS в Enum
            StepType stepType = type switch
            {
                "Text" => StepType.Text,
                "Video" => StepType.Video,
                "Quiz" => StepType.Quiz,
                _ => StepType.Text
            };

            var orders = await _db.Steps
                .Where(s => s.LessonId == lessonId)
                .Select(s => s.Order)
                .ToListAsync();

            int nextOrder = orders.Any() ? orders.Max() + 1 : 1;

            var newStep = new StepModel
            {
                LessonId = lessonId,
                Type = stepType,
                Title = GetDefaultTitle(stepType),
                Order = nextOrder,
                TextContent = stepType == StepType.Text ? "Введите текст лекции..." : "",
                VideoUrl = ""
            };

            _db.Steps.Add(newStep);
            await _db.SaveChangesAsync();

            return Ok();
        }

        // 5. Получение данных урока для редактора (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLessonData(int id)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Steps)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            return Json(new
            {
                title = lesson.Title,
                steps = lesson.Steps.OrderBy(s => s.Order).Select(s => new {
                    id = s.Id,
                    title = s.Title,
                    type = (int)s.Type,
                    textContent = s.TextContent ?? "",
                    videoUrl = s.VideoUrl ?? ""
                })
            });
        }

        // 6. Сохранение урока и всех его шагов
        [HttpPost]
        public async Task<IActionResult> SaveLesson([FromBody] LessonUpdateDto model)
        {
            if (model == null) return BadRequest("Данные не получены");

            var lesson = await _db.Lessons
                .Include(l => l.Steps)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null) return NotFound();

            lesson.Title = model.Title;

            foreach (var stepData in model.Steps)
            {
                var step = lesson.Steps.FirstOrDefault(s => s.Id == stepData.Id);
                if (step != null)
                {
                    step.Title = stepData.Title;
                    step.TextContent = stepData.TextContent;
                    step.VideoUrl = stepData.VideoUrl;
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        // 7. Удаление шага (вызывается из иконки x-circle во вьюхе)
        [HttpPost]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _db.Steps.FindAsync(id);
            if (step == null) return NotFound();

            _db.Steps.Remove(step);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // 8. Удаление курса
        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            var userLogin = HttpContext.Session.GetString("Login");
            if (course.AuthorLogin != userLogin) return Forbid();

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private string GetDefaultTitle(StepType type) => type switch
        {
            StepType.Text => "Новая лекция",
            StepType.Video => "Видео-урок",
            StepType.Quiz => "Тест",
            _ => "Новый шаг"
        };


        // --- МЕТОДЫ ДЛЯ ТЕСТОВ (QUIZ) ---

        [HttpGet]
        public async Task<IActionResult> GetQuizOptions(int stepId)
        {
            var options = await _db.QuizOptions
                .Where(o => o.StepId == stepId)
                .ToListAsync();
            return Json(options);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuizOption(int stepId)
        {
            var option = new QuizOptionModel
            {
                StepId = stepId,
                Text = "Новый вариант ответа",
                IsCorrect = false
            };
            _db.QuizOptions.Add(option);
            await _db.SaveChangesAsync();
            return Json(option);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuizOption(int id, string text, bool isCorrect)
        {
            var option = await _db.QuizOptions.FindAsync(id);
            if (option == null) return NotFound();

            option.Text = text;
            option.IsCorrect = isCorrect;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuizOption(int id)
        {
            var option = await _db.QuizOptions.FindAsync(id);
            if (option == null) return NotFound();

            _db.QuizOptions.Remove(option);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> PublishCourse(int id)
        {
            var course = await _db.Courses.FindAsync(id);

            if (course == null) return NotFound();

            // Проверка прав (только автор)
            var userLogin = HttpContext.Session.GetString("Login");
            if (course.AuthorLogin != userLogin) return Forbid();

            // Переключаем статус
            course.IsPublished = true;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }


    // Классы переноса данных
    public class LessonUpdateDto
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public List<StepUpdateDto> Steps { get; set; } = new();
    }

    public class StepUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? TextContent { get; set; }
        public string? VideoUrl { get; set; }
    }

}