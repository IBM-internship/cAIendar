using System.Diagnostics;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	[Authorize]
	public class HomeController : BaseController
	{
		private readonly GmailEmailService _gmail;

		public HomeController(GmailEmailService gmail)
		{
			_gmail = gmail;
		}

		public IActionResult Index()
		{
			return View();
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
