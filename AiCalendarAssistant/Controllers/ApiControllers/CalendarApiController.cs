using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<ActionResult<List<Event>>> GetAllEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _calendarService.GetEventsAsync(e => e.UserId == userId);
            return Ok(events);
        }

        [HttpPost("range")]
        public async Task<ActionResult<List<Event>>> GetEventsInRange([FromBody] TimeRangeRequest range)
        {
            if (range.End <= range.Start)
                return BadRequest("End must be after start.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _calendarService.GetEventsAsync(e =>
                e.UserId == userId &&
                e.Start < range.End &&
                e.End > range.Start);

            return Ok(events);
        }

        [HttpPost("add")]
        public async Task<ActionResult<int>> AddEvent([FromBody] Event newEvent)
        {
            newEvent.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _calendarService.AddEventAsync(newEvent);
            return Ok(newEvent.Id);
        }

        [HttpDelete("delete")]
        public async Task<ActionResult> DeleteEvent([FromBody] DeleteEventRequest request)
        {
            var existing = await _calendarService.GetEventByIdAsync(request.Id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existing == null || existing.UserId != userId)
                return NotFound($"Event with ID {request.Id} not found or unauthorized.");

            var success = await _calendarService.DeleteEventAsync(request.Id);
            if (!success)
                return NotFound($"Event with ID {request.Id} could not be deleted.");
            return NoContent();
        }

        [HttpPut("replace")]
        public async Task<ActionResult> ReplaceEvent([FromBody] Event updatedEvent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _calendarService.GetEventByIdAsync(updatedEvent.Id);
            if (existing == null || existing.UserId != userId)
                return NotFound($"Event with ID {updatedEvent.Id} not found or unauthorized.");

            updatedEvent.UserId = userId;
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
