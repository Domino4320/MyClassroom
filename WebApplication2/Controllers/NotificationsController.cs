using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ApplicationDBContext _db;

        public NotificationsController(ApplicationDBContext db)
        {
            _db = db;
        }

        private string? GetLogin() => HttpContext.Session.GetString("Login");

        public async Task<IActionResult> Index()
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return RedirectToAction("Index", "Authorization");

            var items = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserLogin == login)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { count = 0 });

            var count = await _db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserLogin == login && !n.IsRead);

            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { items = Array.Empty<object>() });

            var items = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserLogin == login)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    body = n.Body,
                    url = n.Url,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();

            return Json(new { items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false });

            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserLogin == login);
            if (n == null) return Json(new { success = false });

            n.IsRead = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false });

            var unread = await _db.Notifications
                .Where(n => n.UserLogin == login && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false });

            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserLogin == login);
            if (n == null) return Json(new { success = false });

            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var login = GetLogin();
            if (string.IsNullOrEmpty(login))
                return Json(new { success = false });

            var items = await _db.Notifications.Where(n => n.UserLogin == login).ToListAsync();
            if (items.Count == 0) return Json(new { success = true });

            _db.Notifications.RemoveRange(items);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}

