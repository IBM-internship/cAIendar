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

/// <summary>
/// Handles the message round-trip with the LLM, including tool calls.
/// The assistant now has one built-in tool:
///   • get_events_in_time_range – returns all events whose <see cref="Event.Start"/>
///     is inside the inclusive window [start, end].
/// </summary>
public class ChatMessager
{
    private readonly ApplicationDbContext _context;
    private readonly PromptRouter         _router;
    private readonly ICalendarService     _calendarService;

    // ── 1) system prompt ──────────────────────────────────────────────────────────
    private const string SystemPrompt =
        """
        You are the user's personal calendar manager. You help them organise events,
        meetings, notes and tasks.  
        When the user asks for information that requires external data, call one of
        the available tools with a JSON tool_call.  
        """;

    // ── 2) tool definition (JSON) ────────────────────────────────────────────────
    //   The LLM receives this every turn so it knows the tool exists.
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

    // ── 3) ctor ──────────────────────────────────────────────────────────────────
    public ChatMessager(
        ApplicationDbContext context,
        PromptRouter         router,
        ICalendarService     calendarService)
    {
        _context          = context;
        _router           = router;
        _calendarService  = calendarService;
    }

    // ── 4) public entry-point ────────────────────────────────────────────────────
    public async Task<DataMessage> GenerateAssistantMessageAsync(
        Chat               chat,
        CancellationToken  ct = default)
    {
        // 4-a) load prior conversation and prepend system message
        var history = new List<PromptMessage> { new("system", SystemPrompt) };

		var userId = chat.UserId;

        var messages = await _context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        foreach (var m in messages)
            history.Add(new(m.Role.ToString().ToLowerInvariant(), m.Text));

        // 4-b) ── FIRST PASS ─ assistant decides whether to call the tool
        var firstRequest = new PromptRequest(
            history,
            Tools      : EventToolDoc.RootElement,
            ToolChoice : "auto"          // let the model decide
        );

        var firstResponse = await _router.SendAsync(firstRequest, ct);

        // 4-c) If the assistant did NOT request a tool, we can return immediately.
        if (!firstResponse.HasToolCalls)
            return await PersistAssistantReplyAsync(
                chat,
                firstResponse.Content ?? string.Empty,
                messages.Count,
                ct);

        // 4-d) ── TOOL EXECUTION & SECOND PASS ─────────────────────────────────
        foreach (var call in firstResponse.ToolCalls!)
        {
			Console.WriteLine("\n\n\n\n\nAssistant called tool: " + call.Name);
            if (call.Name != "get_events_in_time_range" || call.Name != "get_tasks_in_day_range")
				Console.WriteLine("\n\n\nIgnoring tool call: " + call.Name + "\n\n\n");
                continue; // unknown tool – ignore

			JsonElement args = call.Arguments;          // could be { … }  OR  "{"start": … }"

			if (args.ValueKind == JsonValueKind.String)
			{
				// LLM wrapped the JSON in quotes → unwrap it
				using var tmpDoc = JsonDocument.Parse(args.GetString() ?? "{}");
				args = tmpDoc.RootElement.Clone();      // clone keeps it alive after disposal
			}

			var start = DateTime.Parse(
				args.GetProperty("start").GetString()!,
				null, DateTimeStyles.RoundtripKind);

			var end   = DateTime.Parse(
				args.GetProperty("end").GetString()!,
				null, DateTimeStyles.RoundtripKind);


            // Run the tool
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

            // Append the tool result so the LLM sees it in the *next* round
            history.Add(new("tool", payload, call.Id));
        }

        // Second (and final) round: the model now has the tool output
        var followUpRequest  = new PromptRequest(history);
		var finalResponse    = await _router.SendAsync(followUpRequest, ct);

        return await PersistAssistantReplyAsync(
            chat,
            finalResponse.Content ?? string.Empty,
            messages.Count,
            ct);
    }

    // ── 5) helpers ───────────────────────────────────────────────────────────────
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

