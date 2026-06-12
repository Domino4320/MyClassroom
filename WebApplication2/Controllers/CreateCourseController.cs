using Microsoft.AspNetCore.Mvc;
using WebApplication2.Data;
using WebApplication2.Infrastructure;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Controllers
{
    [RequireTeacher]
    public class CreateCourseController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CreateCourseController(ApplicationDBContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. Страница создания нового курса
        [HttpGet]
        public IActionResult Index()
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            return View("Index", new CourseModel());
        }

        // 2. Страница редактирования существующего курса
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id && c.AuthorLogin == login);
            if (course == null) return NotFound();

            // ЗАЩИТА: Если курс уже опубликован, редактировать метаданные нельзя
            if (course.IsPublished)
            {
                TempData["ErrorMessage"] = "Опубликованный курс нельзя редактировать. Сначала снимите его с публикации.";
                return RedirectToAction("Index", "MyCourses");
            }

            return View("Index", course);
        }

        // 3. Обработка создания
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseModel course, IFormFile? uploadCover)
        {
            return await SaveCourse(course, uploadCover, isNew: true);
        }

        // 4. Обработка сохранения правок
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CourseModel course, IFormFile? uploadCover)
        {
            // Предварительная проверка статуса в БД перед любыми действиями
            var existingCourse = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == course.Id);

            if (existingCourse == null) return NotFound();

            var login = HttpContext.Session.GetString("Login");
            if (existingCourse.AuthorLogin != login) return Forbid();

            if (existingCourse.IsPublished)
            {
                TempData["ErrorMessage"] = "Изменения не сохранены: опубликованный курс защищен от редактирования.";
                return RedirectToAction("Index", "MyCourses");
            }

            return await SaveCourse(course, uploadCover, isNew: false);
        }

        // Вспомогательный метод сохранения
        private async Task<IActionResult> SaveCourse(CourseModel course, IFormFile? imageFile, bool isNew)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            // Убираем проверку полей, которые заполняются автоматически
            ModelState.Remove("AuthorLogin");
            ModelState.Remove("CoverImagePath");

            if (ModelState.IsValid)
            {
                CourseModel courseToSave;

                if (isNew)
                {
                    courseToSave = course;
                    courseToSave.AuthorLogin = login;
                    courseToSave.CreatedAt = DateTime.Now;
                    courseToSave.IsPublished = false; // Новый курс всегда черновик
                    _context.Courses.Add(courseToSave);
                }
                else
                {
                    courseToSave = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Id == course.Id && c.AuthorLogin == login);

                    if (courseToSave == null) return NotFound();
                    if (courseToSave.IsPublished) return BadRequest("Редактирование опубликованного курса запрещено.");

                    // Обновляем только разрешенные поля
                    courseToSave.Title = course.Title;
                    courseToSave.Description = course.Description;
                    courseToSave.Category = course.Category;
                }

                // Логика загрузки изображения
                if (imageFile != null && imageFile.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadPath = Path.Combine(wwwRootPath, "images", "covers");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    // Если редактируем и есть старая обложка — её можно удалить (опционально)
                    // if (!isNew && !string.IsNullOrEmpty(courseToSave.CoverImagePath)) { ... }

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    courseToSave.CoverImagePath = "/images/covers/" + fileName;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "MyCourses");
            }

            // Если модель невалидна — возвращаемся на форму
            return View("Index", course);
        }
    }
}