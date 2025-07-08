using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using System;

internal sealed class UserNoteReader : IUserNoteReader
{
    public Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default)
    {
		// again fetch from the actual notes
        var note = new UserNote(
				Title: "Stefan's Party",
				Body: "next tuesday, 7pm at his place, bring a gift",
				DateCreated: DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd"),
				TimeCreated: TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).ToString("HH:mm"),
				DayOfWeek: DateTime.UtcNow.AddHours(3).DayOfWeek.ToString()
				);
		//print the note layout:
		Console.WriteLine($"Title: {note.Title}");
		Console.WriteLine($"Body: {note.Body}");
		Console.WriteLine($"Date Created: {note.DateCreated}");
		Console.WriteLine($"Time Created: {note.TimeCreated}");
		Console.WriteLine($"Day of Week: {note.DayOfWeek}");

        return Task.FromResult(note);
    }
}
