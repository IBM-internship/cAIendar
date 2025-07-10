namespace PromptingPipeline.Llm;

using PromptingPipeline.Models;

public interface ILlmClient
{
    Task<PromptResponse> SendAsync(PromptRequest request, CancellationToken ct = default);
}

