using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

public class CalendarService : ICalendarService
{
	private readonly ApplicationDbContext _context;

	public CalendarService(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task AddEventAsync(Event calendarEvent)
	{
		_context.Events.Add(calendarEvent);
		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteEventAsync(int eventId)
	{
		var existingEvent = await _context.Events.FindAsync(eventId);
		if (existingEvent == null)
			return false;

		_context.Events.Remove(existingEvent);
		await _context.SaveChangesAsync();
		return true;
	}
	public async Task<Event?> GetEventByIdAsync(int eventId)
	{
		return await _context.Events.FindAsync(eventId);
	}

	public async Task<bool> ReplaceEventAsync(Event updatedEvent) // returns whether event was replaced successfully
	{
		var existingEvent = await _context.Events.FindAsync(updatedEvent.Id);
		if (existingEvent == null)
			return false;

		_context.Entry(existingEvent).CurrentValues.SetValues(updatedEvent);
		await _context.SaveChangesAsync();
		return true;
	}
	public async Task<List<Event>> GetAllEventsAsync()
	{
		return await Task.Run(() => _context.Events.AsNoTracking().ToList());
	}
	public async Task<List<Event>> GetEventsAsync(Func<Event, bool> predicate)
	{
		return await Task.Run(() => _context.Events.AsNoTracking().Where(predicate).ToList());
	}
	public async Task<List<Event>> GetEventsInTimeRangeAsync(DateTime start, DateTime end)
	{
		return await _context.Events
			.AsNoTracking()
			.Where(e => e.Start <= end && e.End >= start)
			.ToListAsync();
	}
}

