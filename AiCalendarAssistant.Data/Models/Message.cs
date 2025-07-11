namespace AiCalendarAssistant.Data.Models;

public class Message
{
	public int Id { get; set; }
	public int ChatId { get; set; }
	public Chat? Chat { get; set; }
	public MessageRole Role { get; set; } //User, Assistant, Tool, System
	public string Text { get; set; } = String.Empty; //Message content
	public int Pos { get; set; } //0,1,2 -> first,second,third message in chat
	public DateTime SentOn { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database

	public string? ToolCalls { get; set; } // JSON array of tool calls, if any
	public string? Name { get; set; } // Name of the tool, if applicable
	public string? ToolCallId { get; set; } // ID of the tool call, if applicable
}
public enum MessageRole
{
	User,
	Assistant,
	Tool,
	System
}
