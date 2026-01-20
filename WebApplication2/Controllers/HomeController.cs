using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var courses = new List<CourseCardModel>
        {
            new CourseCardModel
            {
                CourseName = "Введение в Frontend",
                CourseTeacherName = "Алексей Петров",
                CourseTeacherAvatar = "https://i.pravatar.cc/100?img=5",
                CourseStudentsAmount = 18
            }
        };

            // ?? ОБЯЗАТЕЛЬНО передаём Model
            return View(courses);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
