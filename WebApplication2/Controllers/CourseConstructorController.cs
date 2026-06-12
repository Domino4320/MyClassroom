using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Infrastructure;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RequireTeacher]
    public class CourseConstructorController : Controller
    {
        private readonly ApplicationDBContext _db;
        private readonly IWebHostEnvironment _env;

        public CourseConstructorController(ApplicationDBContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private async Task<(LessonModel? lesson, CourseModel? course, string? error)> GetValidLesson(int lessonId)
        {
            var lesson = await _db.Lessons.Include(l => l.Module).Include(l => l.Steps).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return (null, null, "Урок не найден.");

            var (course, error) = await GetValidCourse(lesson.Module.CourseId);
            if (error != null) return (lesson, null, error);

            return (lesson, course, null);
        }

        // Вспомогательный метод для проверки прав доступа и состояния курса
        private async Task<(CourseModel? course, string? error)> GetValidCourse(int courseId, bool checkPublished = true)
        {
            var userLogin = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(userLogin))
                return (null, "Сессия истекла. Пожалуйста, войдите в аккаунт заново.");

            if (HttpContext.Session.GetString("Role") != "Teacher")
                return (null, "У вас нет прав преподавателя.");

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
            var (lesson, _, error) = await GetValidLesson(id);
            if (error != null) return Forbid();
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
        public async Task<IActionResult> RenameModule([FromQuery] int moduleId, [FromQuery] string title)
        {
            var module = await _db.Modules.FindAsync(moduleId);
            if (module == null) return NotFound();

            var (course, error) = await GetValidCourse(module.CourseId);
            if (error != null) return BadRequest(error);

            var trimmed = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return BadRequest("Название не может быть пустым.");

            module.Title = trimmed;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RenameLesson([FromQuery] int lessonId, [FromQuery] string title)
        {
            var lesson = await _db.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();

            var (course, error) = await GetValidCourse(lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            var trimmed = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return BadRequest("Название не может быть пустым.");

            lesson.Title = trimmed;
            await _db.SaveChangesAsync();
            return Ok(new { title = lesson.Title });
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteModule/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _db.Modules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (module == null) return NotFound();

            var (course, error) = await GetValidCourse(module.CourseId);
            if (error != null) return BadRequest(error);

            foreach (var lesson in module.Lessons)
                await DeleteLessonMaterialFilesAsync(lesson.Id);

            _db.Modules.Remove(module);
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

            await DeleteLessonMaterialFilesAsync(id);

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task DeleteLessonMaterialFilesAsync(int lessonId)
        {
            var fileMaterials = await _db.LessonMaterials
                .Where(m => m.LessonId == lessonId && m.Kind == LessonMaterialKind.File)
                .ToListAsync();

            foreach (var material in fileMaterials)
            {
                if (!string.IsNullOrEmpty(material.StoredPath))
                {
                    var physical = Path.Combine(_env.WebRootPath, material.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical))
                        System.IO.File.Delete(physical);
                }
            }

            var lessonDir = Path.Combine(_env.WebRootPath, "uploads", "lesson-materials", lessonId.ToString());
            if (Directory.Exists(lessonDir))
            {
                try { Directory.Delete(lessonDir, recursive: true); } catch { /* ignore */ }
            }
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteStep/{id}")]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _db.Steps.Include(s => s.Lesson).ThenInclude(l => l.Module).FirstOrDefaultAsync(s => s.Id == id);
            if (step == null) return NotFound();

            var (course, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            DeleteInteractiveImageFolder(id);
            _db.Steps.Remove(step);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [RequestSizeLimit(InteractiveImageHelper.MaxFileBytes)]
        public async Task<IActionResult> UploadInteractiveImage(int stepId, IFormFile file, string? replacePath = null)
        {
            var step = await _db.Steps.Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null) return NotFound();

            var (_, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return BadRequest(new { message = error });

            if (step.Type != StepType.Interactive)
                return BadRequest(new { message = "Загрузка картинок только для интерактивных шагов." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Выберите файл изображения." });

            if (file.Length > InteractiveImageHelper.MaxFileBytes)
                return BadRequest(new { message = "Изображение слишком большое (максимум 5 МБ)." });

            if (!InteractiveImageHelper.IsAllowedExtension(file.FileName))
                return BadRequest(new { message = "Разрешены .jpg, .jpeg, .png, .gif, .webp, .bmp" });

            var config = InteractiveStepHelper.ParseConfig(step.TextContent);
            var currentCount = config?.Options?.Count(o => !string.IsNullOrWhiteSpace(o.ImageUrl)) ?? 0;
            var isReplace = InteractiveImageHelper.IsStoredInteractivePath(stepId, replacePath);

            if (!isReplace && currentCount >= InteractiveImageHelper.MaxImagesPerAssignment)
                return BadRequest(new { message = $"Максимум {InteractiveImageHelper.MaxImagesPerAssignment} картинок в задании." });

            var ext = Path.GetExtension(file.FileName);
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "interactive", stepId.ToString());
            Directory.CreateDirectory(uploadDir);

            var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadDir, storedFileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (isReplace)
            {
                var oldPhysical = InteractiveImageHelper.TryGetPhysicalPath(_env, replacePath);
                if (oldPhysical != null && System.IO.File.Exists(oldPhysical) &&
                    !string.Equals(oldPhysical, physicalPath, StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Delete(oldPhysical);
                }
            }

            var webPath = $"/uploads/interactive/{stepId}/{storedFileName}";
            return Json(new { imageUrl = webPath });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteInteractiveImage(int stepId, [FromQuery] string path)
        {
            var step = await _db.Steps.Include(s => s.Lesson).ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null) return NotFound();

            var (_, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            if (!InteractiveImageHelper.IsStoredInteractivePath(stepId, path))
                return BadRequest(new { message = "Некорректный путь к файлу." });

            var physical = InteractiveImageHelper.TryGetPhysicalPath(_env, path);
            if (physical != null && System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);

            return Ok();
        }

        private void DeleteInteractiveImageFolder(int stepId)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "interactive", stepId.ToString());
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuizOptions(int stepId)
        {
            var step = await _db.Steps
                .Include(s => s.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null) return NotFound();

            var (_, error) = await GetValidCourse(step.Lesson.Module.CourseId);
            if (error != null) return Forbid();

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
            var option = await _db.QuizOptions
                .Include(o => o.Step)
                .ThenInclude(s => s.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (option == null) return NotFound();

            var (_, error) = await GetValidCourse(option.Step.Lesson.Module.CourseId);
            if (error != null) return Forbid();

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
            var option = await _db.QuizOptions
                .Include(o => o.Step)
                .ThenInclude(s => s.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (option == null) return NotFound();

            var (_, error) = await GetValidCourse(option.Step.Lesson.Module.CourseId);
            if (error != null) return Forbid();

            _db.QuizOptions.Remove(option);
            await _db.SaveChangesAsync();
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

        [HttpGet]
        public async Task<IActionResult> GetLessonMaterials(int lessonId)
        {
            var (lesson, _, error) = await GetValidLesson(lessonId);
            if (error != null) return BadRequest(error);
            if (lesson == null) return NotFound();

            var items = await _db.LessonMaterials
                .AsNoTracking()
                .Where(m => m.LessonId == lessonId)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Id)
                .ToListAsync();

            return Json(items.Select(m => new
            {
                m.Id,
                kind = (int)m.Kind,
                m.Title,
                m.FileName,
                url = m.Url,
                downloadUrl = m.Kind == LessonMaterialKind.File && m.StoredPath != null
                    ? Url.Action(nameof(CourseController.DownloadLessonMaterial), "Course", new { id = m.Id })
                    : null
            }));
        }

        [HttpPost]
        [RequestSizeLimit(LessonMaterialHelper.MaxFileBytes)]
        public async Task<IActionResult> UploadLessonMaterial(int lessonId, IFormFile file, string? title)
        {
            var (lesson, _, error) = await GetValidLesson(lessonId);
            if (error != null) return BadRequest(new { message = error });
            if (lesson == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Выберите файл." });

            if (file.Length > LessonMaterialHelper.MaxFileBytes)
                return BadRequest(new { message = "Файл слишком большой (максимум 100 МБ)." });

            if (!LessonMaterialHelper.IsAllowedExtension(file.FileName))
                return BadRequest(new { message = "Разрешены только .pptx, .docx, .pdf, .xlsx, .txt" });

            var fileCount = await _db.LessonMaterials.CountAsync(m =>
                m.LessonId == lessonId && m.Kind == LessonMaterialKind.File);
            if (fileCount >= LessonMaterialHelper.MaxFilesPerLesson)
                return BadRequest(new { message = $"Максимум {LessonMaterialHelper.MaxFilesPerLesson} файлов на урок." });

            var safeName = LessonMaterialHelper.SanitizeFileName(file.FileName);
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "lesson-materials", lessonId.ToString());
            Directory.CreateDirectory(uploadDir);

            var storedFileName = $"{Guid.NewGuid():N}_{safeName}";
            var physicalPath = Path.Combine(uploadDir, storedFileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var nextOrder = (await _db.LessonMaterials.Where(m => m.LessonId == lessonId).MaxAsync(m => (int?)m.Order) ?? 0) + 1;
            var displayTitle = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(safeName) : title.Trim();
            var webPath = $"/uploads/lesson-materials/{lessonId}/{storedFileName}";

            var material = new LessonMaterialModel
            {
                LessonId = lessonId,
                Kind = LessonMaterialKind.File,
                Title = displayTitle,
                FileName = safeName,
                StoredPath = webPath,
                Order = nextOrder
            };

            _db.LessonMaterials.Add(material);
            await _db.SaveChangesAsync();

            return Json(new
            {
                material.Id,
                kind = (int)material.Kind,
                material.Title,
                material.FileName,
                url = (string?)null,
                downloadUrl = Url.Action(nameof(CourseController.DownloadLessonMaterial), "Course", new { id = material.Id })
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddLessonLink([FromBody] AddLessonLinkDto dto)
        {
            if (dto == null || dto.LessonId <= 0)
                return BadRequest(new { message = "Некорректные данные." });

            var (lesson, _, error) = await GetValidLesson(dto.LessonId);
            if (error != null) return BadRequest(new { message = error });
            if (lesson == null) return NotFound();

            if (!LessonMaterialHelper.IsValidHttpUrl(dto.Url))
                return BadRequest(new { message = "Укажите корректную ссылку (http или https)." });

            var linkCount = await _db.LessonMaterials.CountAsync(m =>
                m.LessonId == dto.LessonId && m.Kind == LessonMaterialKind.Link);
            if (linkCount >= LessonMaterialHelper.MaxLinksPerLesson)
                return BadRequest(new { message = $"Максимум {LessonMaterialHelper.MaxLinksPerLesson} ссылок на урок." });

            var linkTitle = (dto.Title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(linkTitle))
                linkTitle = dto.Url!.Trim();

            var nextOrder = (await _db.LessonMaterials.Where(m => m.LessonId == dto.LessonId).MaxAsync(m => (int?)m.Order) ?? 0) + 1;
            var material = new LessonMaterialModel
            {
                LessonId = dto.LessonId,
                Kind = LessonMaterialKind.Link,
                Title = linkTitle,
                Url = dto.Url!.Trim(),
                Order = nextOrder
            };

            _db.LessonMaterials.Add(material);
            await _db.SaveChangesAsync();

            return Json(new
            {
                material.Id,
                kind = (int)material.Kind,
                material.Title,
                fileName = (string?)null,
                url = material.Url,
                downloadUrl = (string?)null
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLessonMaterial([FromBody] UpdateLessonMaterialDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new { message = "Некорректные данные." });

            var material = await _db.LessonMaterials
                .Include(m => m.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(m => m.Id == dto.Id);

            if (material == null) return NotFound();

            var (_, error) = await GetValidCourse(material.Lesson.Module.CourseId);
            if (error != null) return BadRequest(new { message = error });

            var caption = (dto.Title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(caption))
                return BadRequest(new { message = "Укажите подпись." });

            material.Title = caption;
            await _db.SaveChangesAsync();

            return Json(new { id = material.Id, title = material.Title });
        }

        [HttpPost]
        [Route("CourseConstructor/DeleteLessonMaterial/{id}")]
        public async Task<IActionResult> DeleteLessonMaterial(int id)
        {
            var material = await _db.LessonMaterials
                .Include(m => m.Lesson)
                .ThenInclude(l => l.Module)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null) return NotFound();

            var (_, error) = await GetValidCourse(material.Lesson.Module.CourseId);
            if (error != null) return BadRequest(error);

            if (material.Kind == LessonMaterialKind.File && !string.IsNullOrEmpty(material.StoredPath))
            {
                var physical = Path.Combine(_env.WebRootPath, material.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }

            _db.LessonMaterials.Remove(material);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public class AddLessonLinkDto
    {
        public int LessonId { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
    }

    public class UpdateLessonMaterialDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
    }
}