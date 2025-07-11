using AiCalendarAssistant.Config;
using AiCalendarAssistant.Llm;
using AiCalendarAssistant.Models;
using Microsoft.Extensions.Options;
using System;

namespace AiCalendarAssistant.Services;

public sealed class PromptRouter(WatsonxClient w, OllamaClient o, LlmSettings cfg)
{
    private readonly ILlmClient  _watsonx = w;
    private readonly ILlmClient  _ollama = o;
    private readonly LlmSettings _cfg = cfg;
	private readonly string? additionalInstructions = "\nExtra info:\n- Today is " 
        + DateTime.Now.ToString("yyyy-MM-dd") 
        + " (" + DateTime.Now.DayOfWeek + ")";
    public Task<PromptResponse> SendAsync(PromptRequest req, CancellationToken ct = default)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var o = (_cfg.UseOllama ? _ollama : _watsonx).SendAsync(req, ct, additionalInstructions);
                return o;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return null;
    }
}
