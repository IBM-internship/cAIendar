using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface IUserNoteReader
{
    Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default);
}
