using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using WebApplication2.Data;

namespace WebApplication2.Controllers
{
    public class BecomeTeacherController : Controller
    {
        private readonly ApplicationDBContext _context; // Добавлена B

        public BecomeTeacherController(ApplicationDBContext context) // Добавлена B
        {
            _context = context;
        }

        // Метод для отображения страницы с формой
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitApplication(TeacherProfile profile)
        {
            var loginFromSession = HttpContext.Session.GetString("Login");

            // Присваиваем логин
            profile.UserLogin = loginFromSession;

            // Удаляем из валидации, так как в форме этого поля нет (мы пишем его вручную)
            ModelState.Remove("UserLogin");

            if (ModelState.IsValid)
            {
                // Поиск юзера в таблице Users (UserModel)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == loginFromSession);

                if (user != null)
                {
                    user.Role = "Teacher";

                    _context.TeacherProfiles.Add(profile);
                    _context.Users.Update(user);

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetString("Role", "Teacher");

                    return RedirectToAction("Index", "Home");
                }
            }

            // Если данные не валидны (например, не заполнено Required поле)
            return View("Index", profile);
        }
    }
}