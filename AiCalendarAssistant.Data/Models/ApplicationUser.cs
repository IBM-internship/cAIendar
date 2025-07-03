using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public IEnumerable<Event>? Events { get; set; }
    }
}
