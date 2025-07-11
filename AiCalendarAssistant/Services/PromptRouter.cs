using AiCalendarAssistant.Config;
using AiCalendarAssistant.Llm;
using AiCalendarAssistant.Models;
using Microsoft.Extensions.Options;

namespace AiCalendarAssistant.Services;

public sealed class PromptRouter(WatsonxClient w, OllamaClient o, LlmSettings cfg)
{
    private readonly ILlmClient  _watsonx = w;
    private readonly ILlmClient  _ollama = o;
    private readonly LlmSettings _cfg = cfg;

    public Task<PromptResponse> SendAsync(PromptRequest req, CancellationToken ct = default)
        => (_cfg.UseOllama ? _ollama : _watsonx).SendAsync(req, ct);
}
