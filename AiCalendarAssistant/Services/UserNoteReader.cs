using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;

namespace AiCalendarAssistant.Services;

public class UserNoteReader : IUserNoteReader
{
	public Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default)
	{
		// again fetch from the actual notes
		var note = new UserNote
		{
			Title = "Stefan's Party",
			Body = "next tuesday, 7pm at his place, bring a gift",
			CreatedOn = DateTime.UtcNow.AddHours(3),
			IsProcessed=false
		};

		return Task.FromResult(note);
	}
}