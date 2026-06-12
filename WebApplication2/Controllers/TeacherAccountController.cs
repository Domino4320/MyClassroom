using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Infrastructure;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [RequireTeacher]
    public class TeacherAccountController : Controller
    {

        public ApplicationDBContext _db;
        public TeacherAccountController(ApplicationDBContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            // Вместо пустой View вызываем получение данных
            return GetProfileInfo();
        }

        public IActionResult GetProfileInfo()
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return RedirectToAction("Index", "Authorization");

            var user = _db.Users.FirstOrDefault(u => u.Login == login);
            var teacherData = _db.TeacherProfiles.FirstOrDefault(tp => tp.UserLogin == login);

            // Если данных о преподавателе нет в базе, создаем пустой объект, чтобы View не выдавала Error
            if (teacherData == null)
            {
                teacherData = new TeacherProfile
                {
                    UserLogin = login,
                    TeacherTags = "", // Инициализируем пустой строкой
                    About = "Расскажите о себе",
                    CurrentJob = "Не указано",
                    Experience = 0
                };
            }

            var viewModel = new ProfileViewModel
            {
                User = user,
                TeacherInfo = teacherData
            };

            return View("Index", viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateFieldModel model)
        {
            var userLogin = HttpContext.Session.GetString("Login");
            // 2. Проверяем, не пустая ли она
            if (string.IsNullOrEmpty(userLogin))
            {
                return Unauthorized("Сессия истекла или вы не авторизованы");
            }
            var teacherInfo = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.UserLogin == userLogin);
            if (teacherInfo == null) return NotFound("Профиль не найден");

            if (string.IsNullOrWhiteSpace(model.Value) && model.FieldName != "ExtraInfo" && model.FieldName != "PortfolioUrl")
                return BadRequest("Это поле не может быть пустым");

            switch (model.FieldName)
            {
                case "About": teacherInfo.About = model.Value; break;
                case "CurrentJob": teacherInfo.CurrentJob = model.Value; break;
                case "Experience":
                    if (int.TryParse(model.Value, out int exp) && exp >= 0) teacherInfo.Experience = exp;
                    else return BadRequest("Опыт должен быть положительным числом");
                    break;
                case "TeacherTags": teacherInfo.TeacherTags = model.Value; break;
                case "ExtraInfo": teacherInfo.ExtraInfo = model.Value; break;
                case "PortfolioUrl": teacherInfo.PortfolioUrl = model.Value; break;
                default: return BadRequest("Неверное поле");
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        public class UpdateFieldModel
        {
            public string FieldName { get; set; }
            public string Value { get; set; }
        }
    }
}
