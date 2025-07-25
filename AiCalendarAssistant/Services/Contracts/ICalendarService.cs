using AiCalendarAssistant.Data.Models;
using System.Linq.Expressions;

namespace AiCalendarAssistant.Services.Contracts;

public interface ICalendarService
{
    Task AddEventAsync(Event calendarEvent);
    Task<bool> DeleteEventAsync(int eventId);
    Task<Event?> GetEventByIdAsync(int eventId);
    Task<bool> ReplaceEventAsync(Event updatedEvent);
    Task<List<Event>> GetAllEventsAsync(string userId);
    Task<List<Event>> GetEventsAsync(Func<Event, bool> predicate);
    Task<List<Event>> GetEventsInTimeRangeAsync(DateTime start, DateTime end, string userId);
    Task<bool> UpdateEventTimeAsync(int eventId, DateTime newStart, DateTime newEnd);
}
