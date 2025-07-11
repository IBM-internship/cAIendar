namespace AiCalendarAssistant.Models.DTOs;

public class NoteDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public bool IsProcessed { get; set; }
}