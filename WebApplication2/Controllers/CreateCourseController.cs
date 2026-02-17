using Microsoft.AspNetCore.Mvc;
using WebApplication2.Data;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;

namespace WebApplication2.Controllers
{
    public class CreateCourseController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        // В конструкторе мы ДОЛЖНЫ принять эти параметры и присвоить их полям класса
        public CreateCourseController(ApplicationDBContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseModel course, IFormFile? imageFile)
        {
            var login = HttpContext.Session.GetString("Login");

            if (string.IsNullOrEmpty(login))
            {
                // Если логина в сессии нет, значит пользователь не вошел.
                // Вместо создания курса "от анонима", лучше выдать ошибку.
                ModelState.AddModelError("", "Вы должны войти в систему, чтобы создать курс.");
                return View("Index", course);
            }

            // Присваиваем реальный логин из сессии
            course.AuthorLogin = login;
            // 2. Убираем AuthorLogin из проверки ModelState, 
            // чтобы сервер не жаловался, что "поле не заполнено в форме"
            ModelState.Remove("AuthorLogin");

            if (ModelState.IsValid)
            {
                // Логика сохранения картинки (ваш текущий код)
                if (imageFile != null && imageFile.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadPath = Path.Combine(wwwRootPath, @"images\covers");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    course.CoverImagePath = "/images/covers/" + fileName;
                }

                // Устанавливаем дату создания
                course.CreatedAt = DateTime.Now;

                _context.Add(course);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            // Если валидация не прошла, возвращаем ту же вьюху
            return View("Index", course);
        }

    }
}
