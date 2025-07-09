using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Controllers
{
    public class EventsController : BaseController
    {
        private readonly ApplicationDbContext data;

        private readonly UserManager<ApplicationUser> userManager;

        public EventsController(ApplicationDbContext data, UserManager<ApplicationUser> userManager)
        {
            this.data = data;
            this.userManager = userManager;

        }

        [HttpGet]
        public async Task<IActionResult> Create(string start, string end)
        {
            var model = new DetailedEvent
            {
                Start = DateTime.Parse(start),
                End = DateTime.Parse(end)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DetailedEvent model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var currentUser = await userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;

            Event ev = new Event{
                Start = model.Start,
                End = model.End,
                Title = model.Title,
                IsAllDay = model.IsAllDay,
                Description = model.Description,
                Color = model.Color,
                Location = model.Location,
                IsInPerson = model.IsInPerson,
                MeetingLink = model.MeetingLink,
                UserId = currentUserId
            };
            data.Events.Add(ev);
            await data.SaveChangesAsync();

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, DetailedEvent model)
        {
            return View();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            return View();
        }

        [HttpGet]
        public IActionResult Detailed(int id)
        {
            return View();
        }

        
    }
}
