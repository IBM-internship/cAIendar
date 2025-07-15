using System.Security.Claims;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class CalendarApiController : ControllerBase
	{
		private readonly ICalendarService _calendarService;

		public CalendarApiController(ICalendarService calendarService)
		{
			_calendarService = calendarService;
		}

		[HttpGet("all")]
		public async Task<ActionResult<List<object>>> GetAllEvents()
		{
        	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var events = await _calendarService.GetAllEventsAsync(userId);

			var formattedEvents = events.Select(e => new
			{
				id = e.Id.ToString(),
				calendarId = "1",
				title = e.Title,
				category = e.IsAllDay ? "allday" : "time",
				start = e.Start.ToString("o"),
				end = e.End.ToString("o"),
				isAllday = e.IsAllDay,
				location = e.Location,
				raw = new
				{
					description = e.Description,
					meetingLink = e.MeetingLink,
					isInPerson = e.IsInPerson,
					userId = e.UserId
				},
				color = e.Color
			});

			return Ok(formattedEvents);
		}

		[HttpPost("range")]
		public async Task<ActionResult<List<object>>> GetEventsInRange([FromBody] TimeRangeRequest range)
		{
			if (range.End <= range.Start)
				return BadRequest("End must be after start.");

        	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var events = await _calendarService.GetEventsInTimeRangeAsync(range.Start, range.End,userId);

			var formattedEvents = events.Select(e => new
			{
				id = e.Id.ToString(),
				calendarId = "1",
				title = e.Title,
				category = e.IsAllDay ? "allday" : "time",
				start = e.Start.ToString("o"),
				end = e.End.ToString("o"),
				isAllday = e.IsAllDay,
				location = e.Location,
				raw = new
				{
					description = e.Description,
					meetingLink = e.MeetingLink,
					isInPerson = e.IsInPerson,
					userId = e.UserId
				},
				color = e.Color
			});

			return Ok(formattedEvents);
		}

		[HttpPost("add")]
		public async Task<ActionResult<int>> AddEvent([FromBody] Event newEvent)
		{
			Console.WriteLine("event added");
        	var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			newEvent.Start = DateTime.SpecifyKind(newEvent.Start, DateTimeKind.Utc);
			newEvent.End = DateTime.SpecifyKind(newEvent.End, DateTimeKind.Utc);
			newEvent.UserId = userId;
			await _calendarService.AddEventAsync(newEvent);
			return Ok(newEvent.Id);
		}

		[HttpDelete("delete/{id}")]
		public async Task<ActionResult> DeleteEvent(int id)
		{
			Console.WriteLine($"Event deleted: {id}");
			var success = await _calendarService.DeleteEventAsync(id);
			if (!success)
				return NotFound($"Event with ID {id} not found.");
			return NoContent();
		}


		[HttpPut("replace")]
		public async Task<ActionResult> ReplaceEvent([FromBody] Event updatedEvent)
		{
			Console.WriteLine("event replaced");
			var existing = await _calendarService.GetEventByIdAsync(updatedEvent.Id);
			if (existing == null)
				return NotFound($"Event with ID {updatedEvent.Id} not found.");

			updatedEvent.Start = DateTime.SpecifyKind(updatedEvent.Start, DateTimeKind.Utc);
			updatedEvent.End = DateTime.SpecifyKind(updatedEvent.End, DateTimeKind.Utc);

			await _calendarService.ReplaceEventAsync(updatedEvent);
			return NoContent();
		}

		[HttpPut("move")]
		public async Task<IActionResult> UpdateEventTime([FromBody] UpdateTimeRequest update)
		{
			Console.WriteLine("event moved");
			// Ensure UTC handling
			var utcStart = DateTime.SpecifyKind(update.Start, DateTimeKind.Utc);
			var utcEnd = DateTime.SpecifyKind(update.End, DateTimeKind.Utc);
    
			var success = await _calendarService.UpdateEventTimeAsync(update.Id, utcStart, utcEnd);
			if (!success)
				return NotFound();

			return NoContent();
		}

		public class TimeRangeRequest
		{
			public DateTime Start { get; set; }
			public DateTime End { get; set; }
		}

		public class DeleteEventRequest
		{
			public int Id { get; set; }
		}

		public class UpdateTimeRequest
		{
			public int Id { get; set; }
			public DateTime Start { get; set; }
			public DateTime End { get; set; }
		}
	}

}