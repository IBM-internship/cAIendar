using PromptingPipeline.Models;

namespace PromptingPipeline.Interfaces;

internal interface IUserNoteReader
{
    Task<UserNote> GetNextUserNoteAsync(CancellationToken ct = default);
}
