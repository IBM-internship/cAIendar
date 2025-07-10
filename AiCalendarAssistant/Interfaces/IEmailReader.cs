using AiCalendarAssistant.Data.Models;
using PromptingPipeline.Models;

namespace PromptingPipeline.Interfaces;

public interface IEmailReader
{
    Task<Email> GetNextEmailAsync(CancellationToken ct = default);
}
