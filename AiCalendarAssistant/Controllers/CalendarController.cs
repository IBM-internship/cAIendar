using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace AiCalendarAssistant.Controllers;

[Authorize]
public class CalendarController(ApplicationDbContext db) : Controller
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

		var model = new ChatViewModel
		{
			Chats = chats,
			SelectedChat = selectedChat
		};

		// If AJAX request, return partial view only
		if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
		{
			return PartialView("_ChatBox", model);
		}

		return View(model);
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

		// After deletion, reload chat list and partial with no selected chat
		var chats = await db.Chats
			.Include(c => c.Messages.OrderBy(m => m.Pos))
			.Where(c => c.UserId == userId)
			.ToListAsync();

		var model = new ChatViewModel
		{
			Chats = chats,
			SelectedChat = null
		};
		ViewData["SelectedChatId"] = null;

		if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
		{
			return PartialView("_ChatBox", model);
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

		// After creation, reload chats and select the new chat
		var chats = await db.Chats
			.Include(c => c.Messages.OrderBy(m => m.Pos))
			.Where(c => c.UserId == userId)
			.ToListAsync();

		var model = new ChatViewModel
		{
			Chats = chats,
			SelectedChat = chat
		};
		ViewData["SelectedChatId"] = chat.Id;

		if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
		{
			return PartialView("_ChatBox", model);
		}

		return RedirectToAction(nameof(Index), new { selectedChatId = chat.Id });
	}
}

public class ChatViewModel
{
	public List<Chat> Chats { get; set; } = [];
	public Chat? SelectedChat { get; set; }
}
