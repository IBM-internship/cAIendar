using System.Globalization;
using System.Text.Json;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using PromptingPipeline.Models;
using PromptingPipeline.Services;
using DataMessage = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = PromptingPipeline.Models.Message;

namespace PromptingPipeline.Services;

public class ChatMessager
{
    private readonly ApplicationDbContext _context;
    private readonly PromptRouter         _router;
    private readonly ICalendarService     _calendarService;

    private const string SystemPrompt =
        """
        You are the user's personal calendar manager. You help them organise events,
        meetings, notes and tasks.
        When the user asks for information that requires external data, call one of
        the available tools with a JSON tool_call.
        """;

    private static readonly JsonDocument EventToolDoc = JsonDocument.Parse(
        """
        [
          {
            "type": "function",
            "function": {
              "name": "get_events_in_time_range",
              "description": "Fetch all calendar events between two ISO-8601 timestamps (inclusive)",
              "parameters": {
                "type": "object",
                "properties": {
                  "start": { "type": "string", "description": "Start of the window (ISO-8601)" },
                  "end":   { "type": "string", "description": "End of the window (ISO-8601)" }
                },
                "required": ["start", "end"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "get_tasks_in_day_range",
              "description": "Fetch all calendar tasks that the user has between two timestamps days, if you use only for one day, use the same date for start and end",
              "parameters": {
                "type": "object",
                "properties": {
                  "start_day": { "type": "string", "description": "Start Day" },
                  "end_day":   { "type": "string", "description": "End Day" }
                },
                "required": ["start_day", "end_day"]
              }
            }
          }
        ]
        """);

    public ChatMessager(
        ApplicationDbContext context,
        PromptRouter         router,
        ICalendarService     calendarService)
    {
        _context          = context;
        _router           = router;
        _calendarService  = calendarService;
    }

    public async Task<DataMessage> GenerateAssistantMessageAsync(
        Chat               chat,
        CancellationToken  ct = default)
    {
        var history = new List<PromptMessage> { new("system", SystemPrompt) };

		var userId = chat.UserId;

        var messages = await _context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        foreach (var m in messages)
            history.Add(new(m.Role.ToString().ToLowerInvariant(), m.Text));

        var firstRequest = new PromptRequest(
            history,
            Tools      : EventToolDoc.RootElement,
            ToolChoice : "auto"
        );

        var firstResponse = await _router.SendAsync(firstRequest, ct);

        if (!firstResponse.HasToolCalls)
            return await PersistAssistantReplyAsync(
                chat,
                firstResponse.Content ?? string.Empty,
                messages.Count,
                ct);

        foreach (var call in firstResponse.ToolCalls!)
        {
			Console.WriteLine("\n\n\n\n\nAssistant called tool: " + call.Name);
            if (call.Name != "get_events_in_time_range" or call.Name != "get_tasks_in_day_range")
				Console.WriteLine("\n\n\nIgnoring tool call: " + call.Name + "\n\n\n");
                continue;

			JsonElement args = call.Arguments;

			if (args.ValueKind == JsonValueKind.String)
			{
				using var tmpDoc = JsonDocument.Parse(args.GetString() ?? "{}");
				args = tmpDoc.RootElement.Clone();
			}

			var start = DateTime.Parse(
				args.GetProperty("start").GetString()!,
				null, DateTimeStyles.RoundtripKind);

			var end   = DateTime.Parse(
				args.GetProperty("end").GetString()!,
				null, DateTimeStyles.RoundtripKind);


            var events = await _calendarService.GetEventsInTimeRangeAsync(start, end, userId);

            var payload = JsonSerializer.Serialize(new
            {
                events = events.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    start  = e.Start,
                    end    = e.End,
                    e.Location,
                    e.IsAllDay,
                    e.IsInPerson,
                    e.MeetingLink,
                    importance = e.Importance.ToString()
                })
            });

            history.Add(new("tool", payload, call.Id));
        }

        var followUpRequest  = new PromptRequest(history);
		var finalResponse    = await _router.SendAsync(followUpRequest, ct);

        return await PersistAssistantReplyAsync(
            chat,
            finalResponse.Content ?? string.Empty,
            messages.Count,
            ct);
    }

    private async Task<DataMessage> PersistAssistantReplyAsync(
        Chat              chat,
        string            replyText,
        int               position,
        CancellationToken ct)
    {
        var nextMessage = new DataMessage
        {
            ChatId = chat.Id,
            Role   = MessageRole.Assistant,
            Text   = replyText,
            Pos    = position,
            SentOn = DateTime.UtcNow
        };

        _context.Messages.Add(nextMessage);
        await _context.SaveChangesAsync(ct);

        return nextMessage;
    }
}

