using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Interfaces;

public interface IUserNoteReader
{
    Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default);
}
