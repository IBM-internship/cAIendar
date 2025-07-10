using System.Security.Claims;
using AiCalendarAssistant.Models.DTOs;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers.ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarApiController(ICalendarService calendarService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<List<EventDto>>> GetAllEvents()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var events = await calendarService.GetEventsAsync(e => e.UserId == userId);
        var dtoList = events.Select(EventDto.FromEvent).ToList();
        return Ok(dtoList);
    }

    [HttpPost("range")]
    public async Task<ActionResult<List<EventDto>>> GetEventsInRange([FromBody] TimeRangeRequest range)
    {
        if (range.End <= range.Start)
            return BadRequest("End must be after start.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var events = await calendarService.GetEventsAsync(e =>
            e.UserId == userId &&
            e.Start < range.End &&
            e.End > range.Start);

        var dtoList = events.Select(EventDto.FromEvent).ToList();
        return Ok(dtoList);
    }

    [HttpPost("add")]
    public async Task<ActionResult<int>> AddEvent([FromBody] EventDto newEventDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var newEvent = newEventDto.ToEvent(userId);
        await calendarService.AddEventAsync(newEvent);
        return Ok(newEvent.Id);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> DeleteEvent([FromBody] DeleteEventRequest request)
    {
        var existing = await calendarService.GetEventByIdAsync(request.Id);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (existing == null || existing.UserId != userId)
            return NotFound($"Event with ID {request.Id} not found or unauthorized.");

        var success = await calendarService.DeleteEventAsync(request.Id);
        if (!success)
            return NotFound($"Event with ID {request.Id} could not be deleted.");
        return NoContent();
    }

    [HttpPut("replace")]
    public async Task<ActionResult> ReplaceEvent([FromBody] EventDto updatedDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existing = await calendarService.GetEventByIdAsync(updatedDto.Id);
        if (existing == null || existing.UserId != userId)
            return NotFound($"Event with ID {updatedDto.Id} not found or unauthorized.");

        var updatedEvent = updatedDto.ToEvent(userId);
        await calendarService.ReplaceEventAsync(updatedEvent);
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