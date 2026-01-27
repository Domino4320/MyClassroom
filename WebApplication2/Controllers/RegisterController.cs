using Microsoft.AspNetCore.Mvc;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class RegisterController : Controller
    {
        public IActionResult Index()
        {
            return View("Index");
        }

        private readonly ApplicationDBContext _db;
        public RegisterController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult Create(UserModel user)
        {
            bool userExists = _db.Users.Any(u => u.Login == user.Login);

            if (userExists)
            {
                ModelState.AddModelError(nameof(user.Login), "Пользователь с таким логином уже существует");
                return View("Index",user); // возвращаем ТУ ЖЕ форму с моделью
            }

            _db.Users.Add(user);
            _db.SaveChanges();

            return RedirectToAction("Index", "Home");
        }

    }

}
