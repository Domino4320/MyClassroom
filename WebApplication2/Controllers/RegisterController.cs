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
            _db.Users.Add(user);
            _db.SaveChanges();
            return RedirectToAction("Index", "Home");
        }
    }

}
