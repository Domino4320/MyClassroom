using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Services;
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
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel model)
        {
            // Ищем пользователя в БД
            var user = _db.Users
                .FirstOrDefault(u => u.Login == model.Login);

            if (user == null || !PasswordHelper.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("Login", "Неверный логин или пароль");
                ModelState.AddModelError("Password", "Неверный логин или пароль");

                return View("Index",model);
            }

            if (PasswordHelper.NeedsRehash(user.Password))
            {
                user.Password = PasswordHelper.Hash(model.Password);
                _db.SaveChanges();
            }
            HttpContext.Session.SetString("Login", user.Login);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Avatar", user?.Avatar ?? "");


            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
