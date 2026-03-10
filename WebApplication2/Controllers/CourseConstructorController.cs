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

        // Вспомогательный метод для проверки прав и сессии
        private async Task<(CourseModel? course, string? error)> GetValidCourse(int courseId, bool checkPublished = true)
        {
            var userLogin = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(userLogin))
                return (null, "Сессия истекла. Пожалуйста, войдите в аккаунт заново.");

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return (null, "Курс не найден в базе данных.");

            if (course.AuthorLogin != userLogin) return (null, "У вас нет прав на редактирование этого курса.");

            if (checkPublished && course.IsPublished)
                return (null, "Опубликованный курс нельзя редактировать.");

            return (course, null);
        }

        public async Task<IActionResult> Index(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
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
            try
            {
                var (course, error) = await GetValidCourse(courseId);
                if (error != null) return BadRequest(error);

                if (string.IsNullOrWhiteSpace(title)) return BadRequest("Название модуля не может быть пустым.");

                int nextOrder = 1;
                var lastModule = await _db.Modules
                    .Where(m => m.CourseId == courseId)
                    .OrderByDescending(m => m.Order)
                    .FirstOrDefaultAsync();

                if (lastModule != null) nextOrder = lastModule.Order + 1;

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка AddModule: {ex.Message}");
                return StatusCode(500, "Ошибка при сохранении модуля.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddLesson([FromQuery] int moduleId, [FromQuery] string title)
        {
            try
            {
                var module = await _db.Modules.FindAsync(moduleId);
                if (module == null) return NotFound("Модуль не найден.");

                var (course, error) = await GetValidCourse(module.CourseId);
                if (error != null) return BadRequest(error);

                int nextOrder = 1;
                var lastLesson = await _db.Lessons
                    .Where(l => l.ModuleId == moduleId)
                    .OrderByDescending(l => l.Order)
                    .FirstOrDefaultAsync();

                if (lastLesson != null) nextOrder = lastLesson.Order + 1;

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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddStep([FromQuery] int lessonId, [FromQuery] string type)
        {
            try
            {
                var lesson = await _db.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == lessonId);
                if (lesson == null) return NotFound();

                var (course, error) = await GetValidCourse(lesson.Module.CourseId);
                if (error != null) return BadRequest(error);

                StepType stepType = type switch
                {
                    "Video" => StepType.Video,
                    "Quiz" => StepType.Quiz,
                    _ => StepType.Text
                };

                int nextOrder = 1;
                var lastStep = await _db.Steps
                    .Where(s => s.LessonId == lessonId)
                    .OrderByDescending(s => s.Order)
                    .FirstOrDefaultAsync();

                if (lastStep != null) nextOrder = lastStep.Order + 1;

                var newStep = new StepModel
                {
                    LessonId = lessonId,
                    Type = stepType,
                    Title = type == "Video" ? "Видео-урок" : type == "Quiz" ? "Тест" : "Новая лекция",
                    Order = nextOrder,
                    TextContent = stepType == StepType.Text ? "Введите текст..." : "",
                    VideoUrl = "",
                    IsMultipleChoice = false
                };

                _db.Steps.Add(newStep);
                await _db.SaveChangesAsync();

                return Ok(new { id = newStep.Id, type = (int)newStep.Type });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

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
                steps = lesson.Steps.OrderBy(s => s.Order).Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    type = (int)s.Type,
                    textContent = s.TextContent ?? "",
                    videoUrl = s.VideoUrl ?? "",
                    isMultipleChoice = s.IsMultipleChoice
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveLesson([FromBody] LessonUpdateDto model)
        {
            if (model == null) return BadRequest("Нет данных для сохранения");

            var lesson = await _db.Lessons
                .Include(l => l.Steps)
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null) return NotFound();

            var (course, error) = await GetValidCourse(lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            lesson.Title = model.Title;

            if (model.Steps != null)
            {
                foreach (var stepData in model.Steps)
                {
                    var step = lesson.Steps.FirstOrDefault(s => s.Id == stepData.Id);
                    if (step != null)
                    {
                        step.Title = stepData.Title ?? step.Title;
                        step.TextContent = stepData.TextContent;
                        step.VideoUrl = stepData.VideoUrl;
                        step.IsMultipleChoice = stepData.IsMultipleChoice;
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
            var options = await _db.QuizOptions.Where(o => o.StepId == stepId).ToListAsync();
            return Json(options);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuizOption([FromQuery] int stepId)
        {
            var option = new QuizOptionModel
            {
                StepId = stepId,
                Text = "Новый вариант",
                IsCorrect = false
            };
            _db.QuizOptions.Add(option);
            await _db.SaveChangesAsync();
            return Json(option);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuizOption([FromQuery] int id, [FromQuery] string text, [FromQuery] bool isCorrect)
        {
            var option = await _db.QuizOptions.Include(o => o.Step).FirstOrDefaultAsync(o => o.Id == id);
            if (option == null) return NotFound();

            if (isCorrect && !option.Step.IsMultipleChoice)
            {
                var others = await _db.QuizOptions.Where(o => o.StepId == option.StepId && o.Id != id).ToListAsync();
                foreach (var o in others) o.IsCorrect = false;
            }

            option.Text = text;
            option.IsCorrect = isCorrect;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteQuizOption/{id}")]
        public async Task<IActionResult> DeleteQuizOption(int id)
        {
            var option = await _db.QuizOptions.FindAsync(id);
            if (option != null)
            {
                _db.QuizOptions.Remove(option);
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> PublishCourse([FromQuery] int id)
        {
            var (course, error) = await GetValidCourse(id, false);
            if (error != null) return BadRequest(error);

            course.IsPublished = true;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}