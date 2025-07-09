using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services;
using Google.Apis.Gmail.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AiCalendarAssistant.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext data;

        private readonly UserManager<ApplicationUser> userManager;

        public HomeController(ApplicationDbContext data, UserManager<ApplicationUser> userManager)
        {
            this.data = data;
            this.userManager = userManager;
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
