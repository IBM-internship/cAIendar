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
	private string? additionalInstructions = "\nExtra info:\n- Today is " 
        + DateTime.Now.ToString("yyyy-MM-dd") 
		+ " at " + DateTime.Now.AddHours(3).ToString("HH:mm:ss")
        + " (" + DateTime.Now.DayOfWeek + ")";
	// ad duser data here

	private void UpdateTime(){
		additionalInstructions = "\nExtra info:\n- Today is " 
			+ DateTime.Now.ToString("yyyy-MM-dd")
			+ " at " + DateTime.Now.AddHours(3).ToString("HH:mm:ss")
			+ " (" + DateTime.Now.DayOfWeek + ")"
			+ ApplicationDbContext.GetUserD
		// add user data here
	}
    public Task<PromptResponse> SendAsync(PromptRequest req, CancellationToken ct = default)
    {
		UpdateTime();
        for (var i = 0; i < 3; i++)
        {
            try
            {
				Console.WriteLine($"Additional Instructions: {additionalInstructions}");
                var o = (_cfg.UseOllama ? _ollama : _watsonx).SendAsync(req, ct, additionalInstructions);
                return o;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return null;
    }
}
