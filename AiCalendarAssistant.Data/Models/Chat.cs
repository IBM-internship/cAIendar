namespace AiCalendarAssistant.Data.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public IEnumerable<Message>? Messages { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
    }
}