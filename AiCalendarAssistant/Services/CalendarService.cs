using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class CalendarService(ApplicationDbContext context) : ICalendarService
{
	public async Task AddEventAsync(Event calendarEvent)
	{
		context.Events.Add(calendarEvent);
		await context.SaveChangesAsync();
	}

	public async Task<bool> DeleteEventAsync(int eventId)
	{
		var existingEvent = await context.Events.FindAsync(eventId);
		if (existingEvent == null)
			return false;

		context.Events.Remove(existingEvent);
		await context.SaveChangesAsync();
		return true;
	}

	public async Task<Event?> GetEventByIdAsync(int eventId)
	{
		return await context.Events.FindAsync(eventId);
	}

	public async Task<bool> ReplaceEventAsync(Event updatedEvent) // returns whether event was replaced successfully
	{
		var existingEvent = await context.Events.FindAsync(updatedEvent.Id);
		if (existingEvent == null)
			return false;
		string userId = existingEvent.UserId;
		context.Entry(existingEvent).CurrentValues.SetValues(updatedEvent);
		existingEvent.UserId = userId;
		await context.SaveChangesAsync();
		return true;
	}

	public async Task<List<Event>> GetAllEventsAsync(string userId)
	{
		return await Task.Run(() => context.Events.AsNoTracking().Where(e=>e.UserId==userId).ToList());
	}

	public async Task<List<Event>> GetEventsAsync(Func<Event, bool> predicate)
	{
		return await Task.Run(() => context.Events.AsNoTracking().AsEnumerable().Where(predicate).ToList());
	}

	public async Task<List<Event>> GetEventsInTimeRangeAsync(DateTime start, DateTime end, string userId)
	{
		return await context.Events
			.AsNoTracking()
			.Where(e => e.UserId == userId && e.Start <= end && e.End >= start)
			.ToListAsync();
	}
	public async Task<bool> UpdateEventTimeAsync(int eventId, DateTime newStart, DateTime newEnd)
	{
		var existing = await context.Events.FindAsync(eventId);
		if (existing == null)
			return false;
		Console.WriteLine(existing.UserId);
		existing.Start = DateTime.SpecifyKind(newStart, DateTimeKind.Utc);
		existing.End = DateTime.SpecifyKind(newEnd, DateTimeKind.Utc);

		await context.SaveChangesAsync();
		return true;
	}
	
}
