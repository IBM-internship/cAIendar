using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CalendarApiController : ControllerBase
	{
		private readonly ICalendarService _calendarService;

		public CalendarApiController(ICalendarService calendarService)
		{
			_calendarService = calendarService;
		}

		[HttpGet("all")]
		public async Task<ActionResult<List<Event>>> GetAllEvents()
		{
			var events = await _calendarService.GetEventsAsync(e => true);
			return Ok(events);
		}

		[HttpPost("range")]
		public async Task<ActionResult<List<Event>>> GetEventsInRange([FromBody] TimeRangeRequest range)
		{
			if (range.End <= range.Start)
				return BadRequest("End must be after start.");

			var events = await _calendarService.GetEventsInTimeRangeAsync(range.Start, range.End);
			return Ok(events);
		}

		[HttpPost("add")]
		public async Task<ActionResult<int>> AddEvent([FromBody] Event newEvent)
		{
			await _calendarService.AddEventAsync(newEvent);
			return Ok(newEvent.Id);
		}

		[HttpDelete("delete")]
		public async Task<ActionResult> DeleteEvent([FromBody] DeleteEventRequest request)
		{
			var success = await _calendarService.DeleteEventAsync(request.Id);
			if (!success)
				return NotFound($"Event with ID {request.Id} not found.");
			return NoContent();
		}

		[HttpPut("replace")]
		public async Task<ActionResult> ReplaceEvent([FromBody] Event updatedEvent)
		{
			var existing = await _calendarService.GetEventByIdAsync(updatedEvent.Id);
			if (existing == null)
				return NotFound($"Event with ID {updatedEvent.Id} not found.");

			await _calendarService.ReplaceEventAsync(updatedEvent);
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
	}
}