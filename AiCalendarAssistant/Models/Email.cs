namespace AiCalendarAssistant.Models;

public class Email
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
}