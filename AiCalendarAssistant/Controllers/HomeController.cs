using AiCalendarAssistant.Data;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services;
using AiCalendarAssistant.Services.Contracts;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace AiCalendarAssistant.Controllers
{
	[Authorize]
	public class HomeController : BaseController
	{
		private readonly IGmailEmailService _gmail;
		private readonly ApplicationDbContext db;

		public HomeController(IGmailEmailService gmail, ApplicationDbContext _db)
		{
			_gmail = gmail;
			db = _db;
		}

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

		public IActionResult Calendar()
		{
			return View();
		}

		public async Task<IActionResult> Dashboard()
		{
			var emails = await _gmail.GetLastEmailsAsync();
			;
			return View(emails);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}

}
