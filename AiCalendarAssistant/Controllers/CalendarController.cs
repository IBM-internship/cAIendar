using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;


namespace AiCalendarAssistant.Controllers;

[Authorize]
public class CalendarController(ApplicationDbContext db) : BaseController
{
	/*public async Task<IActionResult> Index(int? selectedChatId = null, string? message = null)
	{
        /*var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var chats = await db.Chats
            .Include(c => c.Messages.OrderBy(m => m.Pos))
            .Where(c => c.UserId == userId)
            .ToListAsync();

        // If no chats exist, create one
        if (!chats.Any())
        {
            var newChat = new Chat
            {
                UserId = userId,
                Title = "New Chat"
            };

            db.Chats.Add(newChat);
            await db.SaveChangesAsync();

            // Re-fetch chat with messages (which will be empty initially)
            chats = await db.Chats
                .Include(c => c.Messages.OrderBy(m => m.Pos))
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        var selectedChat = selectedChatId.HasValue
            ? chats.FirstOrDefault(c => c.Id == selectedChatId)
            : chats.FirstOrDefault();

        // If a message is passed, add it to the selected chat
        if (!string.IsNullOrWhiteSpace(message) && selectedChat != null)
        {
            var newMessage = new Message
            {
                ChatId = selectedChat.Id,
                Role = MessageRole.User,
                Text = message,
                SentOn = DateTime.UtcNow,
                Pos = selectedChat.Messages?.Count() ?? 0
            };

            db.Messages.Add(newMessage);
            await db.SaveChangesAsync();

            // Refresh the chat with messages after adding the new message
            selectedChat = await db.Chats
                .Include(c => c.Messages.OrderBy(m => m.Pos))
                .FirstOrDefaultAsync(c => c.Id == selectedChat.Id);
        }

        ViewData["SelectedChatId"] = selectedChat?.Id;

        return View(new ChatViewModel
        {
            Chats = chats,
            SelectedChat = selectedChat
        });
    }*/

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

    [HttpGet]
    public async Task<IActionResult> ChatMessagesPartial(int chatId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var chat = await db.Chats
            .Include(c => c.Messages.OrderBy(m => m.Pos))
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);

        if (chat == null)
            return NotFound();

        var viewModel = new ChatViewModel
        {
            Chats = new List<Chat>(), // Not needed here
            SelectedChat = chat
        };

        ViewData["SelectedChatId"] = chatId;

        return PartialView("_ChatPartial", viewModel);
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