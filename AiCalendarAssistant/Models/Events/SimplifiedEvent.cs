using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Models.Events
{
    public class SimplifiedEvent
    {
        public int Id { get; set; } // Primary Key

        public string Title { get; set; } = null!; // Заглавие на събитието

        public string? Description { get; set; } // Описание (по избор)

        public DateTime Start { get; set; } // Начален час/дата

        public DateTime End { get; set; } // Краен час/дата

        // Ако има потребители в системата
        public string? UserId { get; set; } // За връзка с потребител            
        public ApplicationUser? User { get; set; }
    }
}
