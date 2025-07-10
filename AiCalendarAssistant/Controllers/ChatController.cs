using System.Security.Claims;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ChatController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? selectedChatId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var chats = await _db.Chats
                .Include(c => c.Messages.OrderBy(m => m.Pos))
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var selectedChat = selectedChatId.HasValue
                ? chats.FirstOrDefault(c => c.Id == selectedChatId)
                : chats.FirstOrDefault();

            ViewData["SelectedChatId"] = selectedChat?.Id;

            return View(new ChatViewModel
            {
                Chats = chats,
                SelectedChat = selectedChat
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (chat != null)
            {
                _db.Chats.Remove(chat);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chat = new Chat
            {
                UserId = userId,
                Messages = new List<Message>()
            };

            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { selectedChatId = chat.Id });
        }
    }


    public class ChatViewModel
    {
        public List<Chat> Chats { get; set; } = new();
        public Chat? SelectedChat { get; set; }
    }
}
