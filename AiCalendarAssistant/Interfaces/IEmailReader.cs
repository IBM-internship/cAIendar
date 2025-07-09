using AiCalendarAssistant.Data.Models;
using PromptingPipeline.Models;

namespace PromptingPipeline.Interfaces;

internal interface IEmailReader
{
    Task<Email> GetNextEmailAsync(CancellationToken ct = default);
}
