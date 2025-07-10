using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public IEnumerable<Event>? Events { get; set; }
        public IEnumerable<Email>? Emails { get; set; }
        public IEnumerable<UserNote>? UserNotes { get; set; }
        public IEnumerable<Chat>? Chats { get; set; }
        public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
    }
}
