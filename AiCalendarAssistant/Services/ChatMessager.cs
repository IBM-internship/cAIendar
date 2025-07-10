using System.Text.Json;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using Microsoft.EntityFrameworkCore;
using DataMessage = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public class ChatMessager(ApplicationDbContext context, PromptRouter router, JsonDocument? tools = null)
{
    private const string SystemPrompt = "You are a helpful assistant that manages the user's calendar and communications.";

    public async Task<DataMessage> GenerateAssistantMessageAsync(Chat chat, CancellationToken ct = default)
    {
        // var history = new List<PromptingPipeline.Models.Message> { new("system", SystemPrompt) };
		var history = new List<PromptMessage> { new("system", SystemPrompt) };

        var messages = await context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        foreach (var m in messages)
        {
            history.Add(new(m.Role.ToString().ToLower(), m.Text));
        }

        var prompt = new PromptRequest(history, Tools: tools?.RootElement);
        var response = await router.SendAsync(prompt, ct);
        var replyText = response.Content ?? string.Empty;

        var nextMessage = new DataMessage
        {
            ChatId = chat.Id,
            Role = MessageRole.Assistant,
            Text = replyText,
            Pos = messages.Count,
            SentOn = DateTime.UtcNow
        };

        await AddMessageAsync(nextMessage, ct);
        return nextMessage;
    }

    private async Task AddMessageAsync(DataMessage message, CancellationToken ct)
    {
        context.Messages.Add(message);
        await context.SaveChangesAsync(ct);
    }
}
