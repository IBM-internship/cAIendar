using System.Globalization;
using System.Text.Json;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using PromptingPipeline.Models;
using PromptingPipeline.Services;
using DataMessage  = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = PromptingPipeline.Models.Message;

namespace PromptingPipeline.Services;

public sealed class ChatMessager
{
    private readonly ApplicationDbContext _context;
    private readonly PromptRouter         _router;
    private readonly ICalendarService     _calendarService;

    // ──────────────────────────────────────────────────────────────────────────
    private const string SystemPrompt =
        """
        You are the user's personal calendar manager. You help them organise events,
        meetings, notes and tasks.  
        When the user asks for information that requires external data, call one of
        the available tools with a JSON tool_call.
        """;

    private static readonly JsonDocument ToolDoc = JsonDocument.Parse(
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
                "required": ["start","end"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "get_tasks_in_day_range",
              "description": "Fetch all tasks whose date lies between two calendar days (inclusive). If you need a single day use the same date for start_day and end_day.",
              "parameters": {
                "type": "object",
                "properties": {
                  "start_day": { "type": "string", "description": "YYYY-MM-DD or any ISO-8601 date" },
                  "end_day":   { "type": "string", "description": "YYYY-MM-DD or any ISO-8601 date" }
                },
                "required": ["start_day","end_day"]
              }
            }
          }
        ]
        """);
    // ──────────────────────────────────────────────────────────────────────────

    public ChatMessager(
        ApplicationDbContext context,
        PromptRouter         router,
        ICalendarService     calendarService)
    {
        _context         = context;
        _router          = router;
        _calendarService = calendarService;
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<DataMessage> GenerateAssistantMessageAsync(
        Chat              chat,
        CancellationToken ct = default)
    {
        // 1) build conversation history
        var history = new List<PromptMessage> { new("system", SystemPrompt) };

        var messages = await _context.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.Pos)
            .ToListAsync(ct);

        foreach (var m in messages)
            history.Add(new(m.Role.ToString().ToLowerInvariant(), m.Text));

        // 2) first pass – let the model decide whether to call a tool
        var firstReq = new PromptRequest(
            history,
            Tools      : ToolDoc.RootElement,
            ToolChoice : "auto");

        var firstResp = await _router.SendAsync(firstReq, ct);

        // 3) if no tool call, persist answer & return
        if (!firstResp.HasToolCalls)
            return await PersistAssistantReplyAsync(
                chat, firstResp.Content ?? string.Empty, messages.Count, ct);

        // 4) handle each tool-call, append results
        foreach (var call in firstResp.ToolCalls!)
        {
            var payload = await ExecuteToolCallAsync(call, chat.UserId, ct);
            if (payload is null) continue;           // unknown tool → skip

            history.Add(new("tool", payload, call.Id));
        }

        // 5) second pass – assistant now has the data
        var followUp      = new PromptRequest(history);
        var finalResp     = await _router.SendAsync(followUp, ct);

        return await PersistAssistantReplyAsync(
            chat, finalResp.Content ?? string.Empty, messages.Count, ct);
    }
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string?> ExecuteToolCallAsync(
        ToolCall          call,
        string?           userId,
        CancellationToken ct)
    {
        return call.Name switch
        {
            "get_events_in_time_range" => await HandleGetEventsInTimeRangeAsync(call, userId, ct),
            "get_tasks_in_day_range"   => await HandleGetTasksInDayRangeAsync(call, userId, ct),
            _                          => null   // unknown tool
        };
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static JsonElement NormalizeArguments(JsonElement element)
    {
        // The LLM sometimes returns a JSON-encoded string instead of an object.
        if (element.ValueKind != JsonValueKind.String) return element;

        using var tmp = JsonDocument.Parse(element.GetString() ?? "{}");
        return tmp.RootElement.Clone();   // keep alive after tmp.Dispose()
    }

    private async Task<string> HandleGetEventsInTimeRangeAsync(
        ToolCall          call,
        string?           userId,
        CancellationToken ct)
    {
        var args  = NormalizeArguments(call.Arguments);

        var start = DateTime.Parse(
            args.GetProperty("start").GetString()!,
            null, DateTimeStyles.RoundtripKind);

        var end   = DateTime.Parse(
            args.GetProperty("end").GetString()!,
            null, DateTimeStyles.RoundtripKind);

        // Fetch events via service, then enforce per-user filter (if any)
        var events = await _calendarService.GetEventsInTimeRangeAsync(start, end);
        if (!string.IsNullOrEmpty(userId))
            events = events.Where(e => e.UserId == userId).ToList();

        return JsonSerializer.Serialize(new
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
    }

    private async Task<string> HandleGetTasksInDayRangeAsync(
        ToolCall          call,
        string?           userId,
        CancellationToken ct)
    {
        var args = NormalizeArguments(call.Arguments);

        var startDay = DateOnly.FromDateTime(
            DateTime.Parse(args.GetProperty("start_day").GetString()!,
                           null, DateTimeStyles.RoundtripKind));

        var endDay   = DateOnly.FromDateTime(
            DateTime.Parse(args.GetProperty("end_day").GetString()!,
                           null, DateTimeStyles.RoundtripKind));

        var tasksQry = _context.UserTasks.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            tasksQry = tasksQry.Where(t => t.UserId == userId);

        var tasks = await tasksQry
            .Where(t => t.Date >= startDay && t.Date <= endDay)
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        return JsonSerializer.Serialize(new
        {
            tasks = tasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                date       = t.Date,
                importance = t.Importance.ToString(),
                t.IsCompleted
            })
        });
    }
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<DataMessage> PersistAssistantReplyAsync(
        Chat               chat,
        string             replyText,
        int                position,
        CancellationToken  ct)
    {
        var msg = new DataMessage
        {
            ChatId = chat.Id,
            Role   = MessageRole.Assistant,
            Text   = replyText,
            Pos    = position,
            SentOn = DateTime.UtcNow
        };

        _context.Messages.Add(msg);
        await _context.SaveChangesAsync(ct);
        return msg;
    }
}

