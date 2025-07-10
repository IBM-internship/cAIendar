namespace AiCalendarAssistant.Data.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat? Chat { get; set; }
		public MessageRole Role { get; set; } //User, Assistant, Tool, System
        public string Text { get; set; } = String.Empty; //Message content
        public int Pos { get; set; } //0,1,2 -> first,second,third message in chat
        public DateTime SentOn { get; set; } = DateTime.Now;
    }
	public enum MessageRole
	{
		User,
		Assistant,
		Tool,
		System
	}
}
