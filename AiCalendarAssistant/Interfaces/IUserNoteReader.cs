using AiCalendarAssistant.Data.Models;
using PromptingPipeline.Models;

namespace PromptingPipeline.Interfaces;

public interface IUserNoteReader
{
    Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default);
}
