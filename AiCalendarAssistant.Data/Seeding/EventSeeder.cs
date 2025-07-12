using AiCalendarAssistant.Data.Constants;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Data.Seeding
{
    public class EventSeeder
    {
        public static IEnumerable<Event> SeedEvents()
        {
            List<Event> events = new List<Event>
            {
                new Event
                {
                    Id = EventConstants.Event1Id,
                    Title = EventConstants.Event1Title,
                    Description = EventConstants.Event1Description,
                    Start = new DateTime(2025, 1, 1, 10, 0, 0), // January 1, 2025, 10:00 AM
                    End = new DateTime(2025, 1, 1, 12, 0, 0), // January 1, 2025, 12:00 PM
                    IsAllDay = EventConstants.Event1IsAllDay,
                    Color = EventConstants.Event1Color,
                    Location = EventConstants.Event1Location,
                    IsInPerson = EventConstants.Event1IsInPerson,
                    MeetingLink = EventConstants.Event1MeetingLink,
                    UserId = EventConstants.Event1UserId,
                    EventCreatedFromEmailId = EventConstants.Event1EventCreatedFromEmailId,
                    Importance = EventConstants.Event1Importance,
                    IsDeleted = EventConstants.Event1IsDeleted
                }
            };
            return events;
        }
    }
}
