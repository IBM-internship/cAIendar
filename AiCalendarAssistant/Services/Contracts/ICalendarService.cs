using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts
{
    public interface ICalendarService
    {
        Task AddEventAsync(Event calendarEvent);
        Task<bool> DeleteEventAsync(int eventId);
    }
}
