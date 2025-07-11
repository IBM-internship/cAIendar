using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface ICalendarService
{
    Task AddEventAsync(Event calendarEvent);
    Task<bool> DeleteEventAsync(int eventId);
    Task<Event?> GetEventByIdAsync(int eventId);
    Task<bool> ReplaceEventAsync(Event updatedEvent);
    Task<List<Event>> GetAllEventsAsync();
    Task<List<Event>> GetEventsAsync(Func<Event, bool> predicate);
    Task<List<Event>> GetEventsInTimeRangeAsync(DateTime start, DateTime end, string userId);
}
