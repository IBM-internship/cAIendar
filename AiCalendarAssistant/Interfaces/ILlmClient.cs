using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Interfaces;

public interface ILlmClient
{
    Task<PromptResponse> SendAsync(PromptRequest request, CancellationToken ct = default);
}

