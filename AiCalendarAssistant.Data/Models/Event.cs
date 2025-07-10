namespace AiCalendarAssistant.Data.Models
{
    public class Event
    {
        public int Id { get; set; } // Primary Key

        public string Title { get; set; } = null!; // Заглавие на събитието

        public string? Description { get; set; } // Описание (по избор)

        public DateTime Start { get; set; } // Начален час/дата

        public DateTime End { get; set; } // Краен час/дата

        public bool IsAllDay { get; set; } // Целодневно събитие?

        public string? Color { get; set; } // Hex цветове за различни типове
        public string? Location { get; set; } // Местоположение на събитието
        public bool IsInPerson { get; set; } // Събитието присъствено ли е?
        public string? MeetingLink { get; set; } // Линк за онлайн среща (ако е приложимо)

        // Ако има потребители в системата
        public string? UserId { get; set; } // За връзка с потребител            
        public ApplicationUser? User { get; set; }

        // Ако има имейли в системата
        public int? EventCreatedFromEmailId { get; set; } // За връзка с имейл
        public Email? EventCreatedFromEmail { get; set; } // Because on which email was the event created
        public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
    }
}
