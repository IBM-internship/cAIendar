using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        List<Event> Events { get; set; } = new List<Event>();
    }
}
