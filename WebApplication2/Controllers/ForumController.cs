using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class ForumController : Controller
    {
        private readonly ApplicationDBContext _db;

        public ForumController(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var discussions = await _db.ForumDiscussions
                .Include(d => d.Author)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(discussions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return Unauthorized();

            title = (title ?? "").Trim();
            if (title.Length < 3) return RedirectToAction(nameof(Index));

            _db.ForumDiscussions.Add(new ForumDiscussionModel
            {
                Title = title,
                AuthorLogin = login,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Discussion(int id)
        {
            var discussion = await _db.ForumDiscussions
                .Include(d => d.Author)
                .Include(d => d.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discussion == null) return NotFound();

            return View(discussion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMessage(int discussionId, string text, int? parentMessageId)
        {
            var login = HttpContext.Session.GetString("Login");
            if (string.IsNullOrEmpty(login)) return Unauthorized();

            text = (text ?? "").Trim();
            if (text.Length < 1) return RedirectToAction(nameof(Discussion), new { id = discussionId });

            if (parentMessageId.HasValue)
            {
                var parent = await _db.ForumMessages
                    .FirstOrDefaultAsync(m => m.Id == parentMessageId.Value && m.DiscussionId == discussionId);
                if (parent == null) parentMessageId = null;
            }

            _db.ForumMessages.Add(new ForumMessageModel
            {
                DiscussionId = discussionId,
                UserLogin = login,
                Text = text,
                ParentMessageId = parentMessageId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Discussion), new { id = discussionId });
        }
    }
}

