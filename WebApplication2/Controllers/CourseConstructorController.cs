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

        // Вспомогательный метод для проверки прав доступа и состояния курса
        private async Task<(CourseModel? course, string? error)> GetValidCourse(int courseId, bool checkPublished = true)
        {
            var userLogin = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(userLogin))
                return (null, "Сессия истекла. Пожалуйста, войдите в аккаунт заново.");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return (null, "Курс не найден.");
            if (course.AuthorLogin != userLogin) return (null, "У вас нет прав на редактирование этого курса.");

            if (checkPublished && course.IsPublished)
                return (null, "Опубликованный курс нельзя редактировать. Сначала снимите его с публикации.");

            return (course, null);
        }

        public async Task<IActionResult> Index(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var userLogin = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(userLogin) || course.AuthorLogin != userLogin) return Forbid();

            if (course.IsPublished)
            {
                TempData["ErrorMessage"] = "Курс опубликован и закрыт для редактирования.";
                return RedirectToAction("Index", "MyCourses");
            }

            var viewModel = new CourseConstructorViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                IsPublished = course.IsPublished,
                Modules = course.Modules?.OrderBy(m => m.Order).ToList() ?? new List<ModuleModel>()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddModule([FromQuery] int courseId, [FromQuery] string title)
        {
            var (course, error) = await GetValidCourse(courseId);
            if (error != null) return BadRequest(error);

            int nextOrder = (await _db.Modules.Where(m => m.CourseId == courseId).MaxAsync(m => (int?)m.Order) ?? 0) + 1;

            var newModule = new ModuleModel { CourseId = courseId, Title = title, Order = nextOrder };
            _db.Modules.Add(newModule);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddLesson([FromQuery] int moduleId, [FromQuery] string title)
        {
            var module = await _db.Modules.FindAsync(moduleId);
            if (module == null) return NotFound("Модуль не найден.");

            var (course, error) = await GetValidCourse(module.CourseId);
            if (error != null) return BadRequest(error);

            int nextOrder = (await _db.Lessons.Where(l => l.ModuleId == moduleId).MaxAsync(l => (int?)l.Order) ?? 0) + 1;

            var newLesson = new LessonModel { ModuleId = moduleId, Title = title, Order = nextOrder };
            _db.Lessons.Add(newLesson);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddStep([FromQuery] int lessonId, [FromQuery] string type)
        {
            var lesson = await _db.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();

            var (course, error) = await GetValidCourse(lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            StepType stepType = type switch
            {
                "Video" => StepType.Video,
                "Quiz" => StepType.Quiz,
                "Interactive" => StepType.Interactive,
                _ => StepType.Text
            };

            int nextOrder = await _db.Steps.CountAsync(s => s.LessonId == lessonId);

            var defaultInteractiveJson = """
                {"kind":"match","instruction":"Сопоставьте термины с определениями","pairs":[{"left":"HTML","right":"Разметка"},{"left":"CSS","right":"Стили"}]}
                """;

            var newStep = new StepModel
            {
                LessonId = lessonId,
                Type = stepType,
                Title = type switch
                {
                    "Video" => "Видео-урок",
                    "Quiz" => "Тест",
                    "Interactive" => "Интерактивное задание",
                    _ => "Новая лекция"
                },
                Order = nextOrder,
                TextContent = stepType switch
                {
                    StepType.Text => "Введите текст...",
                    StepType.Interactive => defaultInteractiveJson,
                    _ => ""
                },
                VideoUrl = "",
                IsMultipleChoice = false,
                IsManualCheck = false,
                CorrectTextAnswer = "",
                MaxPoints = stepType == StepType.Quiz ? 1 : 0
            };

            _db.Steps.Add(newStep);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                id = newStep.Id,
                type = (int)newStep.Type,
                title = newStep.Title,
                textContent = newStep.TextContent ?? "",
                videoUrl = newStep.VideoUrl ?? "",
                isMultipleChoice = newStep.IsMultipleChoice,
                isManualCheck = newStep.IsManualCheck,
                correctTextAnswer = newStep.CorrectTextAnswer ?? "",
                maxPoints = newStep.MaxPoints,
                order = newStep.Order
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLessonData(int id)
        {
            var lesson = await _db.Lessons.Include(l => l.Steps).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            return Json(new
            {
                title = lesson.Title,
                steps = lesson.Steps.OrderBy(s => s.Order).ThenBy(s => s.Id).Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    type = (int)s.Type,
                    textContent = s.TextContent ?? "",
                    videoUrl = s.VideoUrl ?? "",
                    codeTemplate = s.CodeTemplate ?? "",
                    expectedOutput = s.ExpectedOutput ?? "",
                    isMultipleChoice = s.IsMultipleChoice,
                    isManualCheck = s.IsManualCheck,
                    correctTextAnswer = s.CorrectTextAnswer ?? "",
                    maxPoints = s.MaxPoints
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveLesson([FromBody] LessonUpdateDto model)
        {
            if (model == null || model.Steps == null) return BadRequest("Некорректные данные.");

            var lesson = await _db.Lessons
                .Include(l => l.Steps)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null) return NotFound();

            var module = await _db.Modules.FindAsync(lesson.ModuleId);
            var (course, error) = await GetValidCourse(module!.CourseId);
            if (error != null) return BadRequest(error);

            var validationError = await CourseContentValidator.ValidateLessonAsync(lesson, model.Title, model.Steps, _db);
            if (validationError != null) return BadRequest(validationError);

            lesson.Title = model.Title;

            for (var i = 0; i < model.Steps.Count; i++)
            {
                var stepDto = model.Steps[i];
                var step = lesson.Steps.FirstOrDefault(s => s.Id == stepDto.Id);
                if (step != null)
                {
                    step.Order = i;
                    step.Title = stepDto.Title ?? step.Title;
                    step.TextContent = step.Type == StepType.Interactive
                        ? InteractiveStepHelper.NormalizeConfigJson(stepDto.TextContent)
                        : (stepDto.TextContent ?? "");
                    step.VideoUrl = stepDto.VideoUrl ?? "";
                    step.CodeTemplate = stepDto.CodeTemplate ?? step.CodeTemplate;
                    step.ExpectedOutput = stepDto.ExpectedOutput ?? step.ExpectedOutput;
                    step.IsMultipleChoice = stepDto.IsMultipleChoice;
                    step.IsManualCheck = stepDto.IsManualCheck;
                    step.CorrectTextAnswer = stepDto.CorrectTextAnswer ?? "";
                    // Баллы должны быть только за тесты (и ручные, и автоматические)
                    if (step.Type == StepType.Quiz)
                    {
                        step.MaxPoints = stepDto.MaxPoints > 0 ? stepDto.MaxPoints : 1;
                    }
                    else
                    {
                        step.MaxPoints = 0;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteLesson/{id}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _db.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            var (course, error) = await GetValidCourse(lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteStep/{id}")]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _db.Steps.Include(s => s.Lesson).ThenInclude(l => l.Module).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null) return NotFound();

            var (course, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            _db.Steps.Remove(step);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetQuizOptions(int stepId)
        {
            return Json(await _db.QuizOptions.Where(o => o.StepId == stepId).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddQuizOption([FromQuery] int stepId)
        {
            var step = await _db.Steps.Include(s => s.Lesson).ThenInclude(l => l.Module).FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null) return NotFound();

            var (course, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            var option = new QuizOptionModel { StepId = stepId, Text = "Новый вариант", IsCorrect = false };
            _db.QuizOptions.Add(option);
            await _db.SaveChangesAsync();
            return Json(option);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuizOption([FromQuery] int id, [FromQuery] string text, [FromQuery] bool isCorrect)
        {
            var option = await _db.QuizOptions.Include(o => o.Step).FirstOrDefaultAsync(o => o.Id == id);
            if (option == null) return NotFound();

            // Если это единственный выбор, снимаем галочки с остальных
            if (isCorrect && !option.Step.IsMultipleChoice)
            {
                var others = await _db.QuizOptions.Where(o => o.StepId == option.StepId && o.Id != id).ToListAsync();
                foreach (var o in others) o.IsCorrect = false;
            }

            option.Text = text ?? "";
            option.IsCorrect = isCorrect;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteQuizOption/{id}")]
        public async Task<IActionResult> DeleteQuizOption(int id)
        {
            var option = await _db.QuizOptions.FindAsync(id);
            if (option != null) { _db.QuizOptions.Remove(option); await _db.SaveChangesAsync(); }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> PublishCourse(int id)
        {
            var (course, error) = await GetValidCourse(id, checkPublished: false);
            if (error != null) return BadRequest(error);

            var courseWithData = await _db.Courses
                .Include(c => c.Modules).ThenInclude(m => m.Lessons).ThenInclude(l => l.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            var publishError = await CourseContentValidator.ValidateCourseForPublishAsync(courseWithData!, _db);
            if (publishError != null) return BadRequest(publishError);

            courseWithData!.IsPublished = true;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}