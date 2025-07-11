using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class NoteService(ApplicationDbContext context) : INoteService
{
    public async Task<List<UserNote>> GetAllNotesAsync()
    {
        return await context.UserNotes.Include(n => n.User).ToListAsync();
    }

    public async Task<List<UserNote>> GetNotesByUserIdAsync(string userId)
    {
        return await context.UserNotes
            .Where(n => n.UserId == userId)
            .Include(n => n.User)
            .ToListAsync();
    }

    public async Task AddNoteAsync(UserNote note)
    {
        context.UserNotes.Add(note);
        await context.SaveChangesAsync();
    }
}