namespace AiCalendarAssistant.Models;

public class Email
{
    public DateTime Date { get; set; }
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
}