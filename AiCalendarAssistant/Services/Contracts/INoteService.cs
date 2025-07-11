using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface INoteService
{
    Task<List<UserNote>> GetAllNotesAsync();
    Task<List<UserNote>> GetNotesByUserIdAsync(string userId);
    Task AddNoteAsync(UserNote note);
}