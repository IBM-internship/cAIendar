using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;
using PromptingPipeline.Models;
using PromptingPipeline.Services;
using System.Text.Json;

using DataMessage = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = PromptingPipeline.Models.Message;

namespace PromptingPipeline.Services;

internal sealed class ChatMessager
{
    private readonly ApplicationDbContext _context;
    private readonly PromptRouter _router;
    private readonly JsonDocument? _tools;

    private const string SystemPrompt = "You are a helpful assistant that manages the user's calendar and communications.";

    public ChatMessager(ApplicationDbContext context, PromptRouter router, JsonDocument? tools = null)
    {
        _context = context;
        _router = router;
        _tools = tools;
    }

    public async Task<DataMessage> GenerateAssistantMessageAsync(Chat chat, CancellationToken ct = default)
    {
        // var history = new List<PromptingPipeline.Models.Message> { new("system", SystemPrompt) };
		var history = new List<PromptMessage> { new("system", SystemPrompt) };

        var messages = await _context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        foreach (var m in messages)
        {
            history.Add(new(m.Role.ToString().ToLower(), m.Text));
        }

        var prompt = new PromptRequest(history, Tools: _tools?.RootElement);
        var response = await _router.SendAsync(prompt, ct);
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
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(ct);
    }
}
