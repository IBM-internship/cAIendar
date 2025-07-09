using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Controllers
{
	public class CalendarController : BaseController
	{
        private readonly ApplicationDbContext data;

        private readonly UserManager<ApplicationUser> userManager;

        public CalendarController(ApplicationDbContext data, UserManager<ApplicationUser> userManager)
        {
            this.data = data;
            this.userManager = userManager;

        }
        public IActionResult Index()
		{
			return View();
		}

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> All()
        {
            var currentUser = await userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;

            var events = data.Events.Where(e => e.UserId == currentUserId).Select(e => new 
            {
                e.Id,
                e.Title,
                e.Start, // ISO 8601
                e.End,
                e.IsAllDay,
                e.Description,
                e.Color,
                e.Location,
                e.IsInPerson,
                e.MeetingLink,
                e.UserId
            }).ToList();

            return new JsonResult(events);
        }
    }
}
