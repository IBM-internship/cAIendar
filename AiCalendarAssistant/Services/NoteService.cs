using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class NoteService(ApplicationDbContext db) : INoteService
{
    public async Task<List<UserNote>> GetAllNotesAsync()
    {
        return await db.UserNotes.Include(n => n.User).ToListAsync();
    }

    public async Task<List<UserNote>> GetNotesByUserIdAsync(string userId)
    {
        return await db.UserNotes
            .Where(n => n.UserId == userId)
            .Include(n => n.User)
            .ToListAsync();
    }

    public async Task AddNoteAsync(UserNote note)
    {
        db.UserNotes.Add(note);
        await db.SaveChangesAsync();
    }
}