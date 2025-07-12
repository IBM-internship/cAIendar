using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Data.Constants
{
    public class EventConstants
    {
        public const int Event1Id = -1;
        public const string Event1Title = "Event 1";
        public const string Event1Description = "This is the first event.";
        public DateTime Event1Start = new DateTime(2025, 1, 1, 10, 0, 0); // January 1, 2025, 10:00 AM
        public DateTime Event1End = new DateTime(2025, 1, 1, 12, 0, 0); // January 1, 2025, 12:00 AM
        public const bool Event1IsAllDay = false;
        public const string Event1Color = "#FF5733"; // Hex color for the event
        public const string Event1Location = "Conference Room A";
        public const bool Event1IsInPerson = true;
        public const string Event1MeetingLink = "";
        public const string Event1UserId = "88bd4ce9-aece-4378-b9d5-1e5cff74b80c"; // User ID from UserConstants
        public const int Event1EventCreatedFromEmailId = -1;
        public const Importance Event1Importance = Importance.Medium; // Default importance
        public const bool Event1IsDeleted = false;
    }
}
