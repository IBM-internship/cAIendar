using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;

public class NoteService : INoteService
{
    private readonly ApplicationDbContext _context;

    public NoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserNote>> GetAllNotesAsync()
    {
        return await _context.UserNotes.Include(n => n.User).ToListAsync();
    }

    public async Task<List<UserNote>> GetNotesByUserIdAsync(string userId)
    {
        return await _context.UserNotes
            .Where(n => n.UserId == userId)
            .Include(n => n.User)
            .ToListAsync();
    }

    public async Task AddNoteAsync(UserNote note)
    {
        _context.UserNotes.Add(note);
        await _context.SaveChangesAsync();
    }
}
