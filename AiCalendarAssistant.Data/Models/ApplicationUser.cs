using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string? UserDiscription { get; set; } = string.Empty; // Description of the user
    public IEnumerable<Event>? Events { get; set; }
    public IEnumerable<Email>? Emails { get; set; }
    public IEnumerable<UserNote>? UserNotes { get; set; }
    public IEnumerable<Chat>? Chats { get; set; }
    public IEnumerable<UserTask>? Tasks{ get; set; }
    public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
}
