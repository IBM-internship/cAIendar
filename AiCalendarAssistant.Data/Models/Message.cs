namespace AiCalendarAssistant.Data.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat? Chat { get; set; }
        public string Text { get; set; } = String.Empty; //Message content
        public int Pos { get; set; } //0,1,2 -> first,second,third message in chat
        public DateTime SentOn { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
    }
}