using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Models;

public class ApplicationUser : IdentityUser
{
    public IEnumerable<Event>? Events { get; set; }
    public IEnumerable<Email>? Emails { get; set; }
    public IEnumerable<UserNote>? UserNotes { get; set; }
    public IEnumerable<Chat>? Chats { get; set; }
    public IEnumerable<UserTask>? Tasks{ get; set; }
}