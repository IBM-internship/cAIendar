using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services
{
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
    }
}
