using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;


namespace WebApplication2.Controllers
{
    public class AuthorizationController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }

        private readonly ApplicationDBContext _db;
        public AuthorizationController(ApplicationDBContext db)
        {
            _db = db;
        }


        [HttpPost]
        public IActionResult Login(UserModel model)
        {
            // Ищем пользователя в БД
            var user = _db.Users
                .FirstOrDefault(u => u.Login == model.Login && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("Login", "Неверный логин или пароль");
                ModelState.AddModelError("Password", "Неверный логин или пароль");

                return View("Index",model);
            }
            HttpContext.Session.SetString("Login", user.Login);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Avatar", user?.Avatar ?? "");


            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
