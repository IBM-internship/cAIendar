using AiCalendarAssistant.Data.Models;

public interface INoteService
{
    Task<List<UserNote>> GetAllNotesAsync();
    Task<List<UserNote>> GetNotesByUserIdAsync(string userId);
    Task AddNoteAsync(UserNote note);
}
