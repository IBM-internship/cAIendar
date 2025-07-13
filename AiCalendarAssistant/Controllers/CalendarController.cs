using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace AiCalendarAssistant.Controllers;

[Authorize]
public class CalendarController(ApplicationDbContext db) : BaseController
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

    public async Task<IActionResult> ChatMessagesPartial(int chatId)
    {

        var selectedChat = await db.Chats
        .Include(c => c.Messages)
        .FirstOrDefaultAsync(c => c.Id == chatId);

        if (selectedChat == null)
            return NotFound();

        var model = new ChatViewModel
        {
            SelectedChat = selectedChat
        };

        ViewData["SelectedChatId"] = chatId;

        return PartialView("_ChatPartial", model);
    }
}