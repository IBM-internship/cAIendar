using System.Security.Claims;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Controllers;

[Authorize]
public class ChatController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? selectedChatId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var chats = await db.Chats
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
        var chat = await db.Chats.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (chat != null)
        {
            db.Chats.Remove(chat);
            await db.SaveChangesAsync();
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

        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { selectedChatId = chat.Id });
    }
}


public class ChatViewModel
{
    public List<Chat> Chats { get; set; } = [];
    public Chat? SelectedChat { get; set; }
}