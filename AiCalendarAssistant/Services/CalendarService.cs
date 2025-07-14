using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class CalendarService(ApplicationDbContext db) : ICalendarService
{
	public async Task AddEventAsync(Event calendarEvent)
	{
		db.Events.Add(calendarEvent);
		await db.SaveChangesAsync();
	}

	public async Task<bool> DeleteEventAsync(int eventId)
	{
		var existingEvent = await db.Events.FindAsync(eventId);
		if (existingEvent == null)
			return false;

		db.Events.Remove(existingEvent);
		await db.SaveChangesAsync();
		return true;
	}

	public async Task<Event?> GetEventByIdAsync(int eventId)
	{
		return await db.Events.FindAsync(eventId);
	}

	public async Task<bool> ReplaceEventAsync(Event updatedEvent) // returns whether event was replaced successfully
	{
		var existingEvent = await db.Events.FindAsync(updatedEvent.Id);
		if (existingEvent == null)
			return false;
		var userId = existingEvent.UserId!;
		db.Entry(existingEvent).CurrentValues.SetValues(updatedEvent);
		existingEvent.UserId = userId;
		await db.SaveChangesAsync();
		return true;
	}

	public async Task<List<Event>> GetAllEventsAsync(string userId)
	{
		return await Task.Run(() => db.Events.AsNoTracking().Where(e=>e.UserId==userId).ToList());
	}

	public async Task<List<Event>> GetEventsAsync(Func<Event, bool> predicate)
	{
		return await Task.Run(() => db.Events.AsNoTracking().AsEnumerable().Where(predicate).ToList());
	}

	public async Task<List<Event>> GetEventsInTimeRangeAsync(DateTime start, DateTime end, string userId)
	{
		return await db.Events
			.AsNoTracking()
			.Where(e => e.UserId == userId && e.Start <= end && e.End >= start)
			.ToListAsync();
	}
	public async Task<bool> UpdateEventTimeAsync(int eventId, DateTime newStart, DateTime newEnd)
	{
		var existing = await db.Events.FindAsync(eventId);
		if (existing == null)
			return false;
		Console.WriteLine(existing.UserId);
		existing.Start = DateTime.SpecifyKind(newStart, DateTimeKind.Utc);
		existing.End = DateTime.SpecifyKind(newEnd, DateTimeKind.Utc);

		await db.SaveChangesAsync();
		return true;
	}
	
}
