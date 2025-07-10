using System.Globalization;
using System.Text.Json;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using Microsoft.EntityFrameworkCore;
using DataMessage = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

/// <summary>
/// Handles the message round-trip with the LLM, including tool calls.
/// The assistant now has one built-in tool:
///   • get_events_in_time_range – returns all events whose <see cref="Event.Start"/>
///     is inside the inclusive window [start, end].
/// </summary>
public class ChatMessager(
    ApplicationDbContext context,
    PromptRouter router,
    ICalendarService calendarService)
{
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
          }
        ]
        """);

    // ── 3) ctor ──────────────────────────────────────────────────────────────────

    // ── 4) public entry-point ────────────────────────────────────────────────────
    public async Task<DataMessage> GenerateAssistantMessageAsync(
        Chat               chat,
        CancellationToken  ct = default)
    {
        // 4-a) load prior conversation and prepend system message
        var history = new List<PromptMessage> { new("system", SystemPrompt) };

		var userId = chat.UserId;

        var messages = await context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        history.AddRange(messages.Select(m => new PromptMessage(m.Role.ToString().ToLowerInvariant(), m.Text)));

        // 4-b) ── FIRST PASS ─ assistant decides whether to call the tool
        var firstRequest = new PromptRequest(
            history,
            Tools      : EventToolDoc.RootElement,
            ToolChoice : "auto"          // let the model decide
        );

        var firstResponse = await router.SendAsync(firstRequest, ct);

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
            if (call.Name != "get_events_in_time_range")
                continue; // unknown tool – ignore

            // Parse arguments
            // var argsDoc = JsonDocument.Parse(call.Arguments);
            // var start   = DateTime.Parse(
            //     argsDoc.RootElement.GetProperty("start").GetString()!,
            //     null, DateTimeStyles.RoundtripKind);
            //
            // var end     = DateTime.Parse(
            //     argsDoc.RootElement.GetProperty("end").GetString()!,
            //     null, DateTimeStyles.RoundtripKind);
			// ─── parse arguments ────────────────────────────────────────────────
			// // ─── parse arguments ──────────────────────────────────────────────
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
            var events = await calendarService.GetEventsInTimeRangeAsync(start, end, userId);

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
		var finalResponse    = await router.SendAsync(followUpRequest, ct);

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

        context.Messages.Add(nextMessage);
        await context.SaveChangesAsync(ct);

        return nextMessage;
    }
}

