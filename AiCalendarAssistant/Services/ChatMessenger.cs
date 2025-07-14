using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using DataMessage = AiCalendarAssistant.Data.Models.Message;
using PromptMessage = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public sealed class ChatMessenger(
    ApplicationDbContext context,
    PromptRouter router,
    ICalendarService calendarService,
  // ITaskService taskService)
  // private readonly GoogleSearchService _googleSearchService;
  ITaskService taskService,
  GoogleSearchService _googleSearchService)
{
  // ──────────────────────────────────────────────────────────────────────────
  private const string SystemPrompt =
      """
        You are the user's personal calendar manager. You help them organise events,
        meetings and tasks.  
        When the user asks for information that requires external data, call one of
        the available tools with a JSON tool_call.
        If the user asks you to do something that requires mutliple steps, you can
        call a tool to fetch the data, then use the data to call another tool and 
        so on until you have done everything to complete the task or have gathered
        enough information to answer the user's question.
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
          },
          {
            "type": "function",
            "function": {
              "name": "create_event",
              "description": "Create a new calendar event",
              "parameters": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" },
                  "description": { "type": "string" },
                  "start": { "type": "string", "description": "ISO-8601 datetime" },
                  "end":   { "type": "string", "description": "ISO-8601 datetime" },
                  "location": { "type": "string" },
                  "is_all_day": { "type": "boolean" },
                  "is_in_person": { "type": "boolean" },
                  "meeting_link": { "type": "string" },
                  "importance": { "type": "string", "enum": ["Low","Normal","High"] }
                },
                "required": ["title","start","end"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "update_event",
              "description": "Replace an existing event's details",
              "parameters": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" },
                  "title": { "type": "string" },
                  "description": { "type": "string" },
                  "start": { "type": "string", "description": "ISO-8601 datetime" },
                  "end":   { "type": "string", "description": "ISO-8601 datetime" },
                  "location": { "type": "string" },
                  "is_all_day": { "type": "boolean" },
                  "is_in_person": { "type": "boolean" },
                  "meeting_link": { "type": "string" },
                  "importance": { "type": "string", "enum": ["Low","Normal","High"] },
                  "color": { "type": "string", "description": "Optional color for the event with words" }
                },
                "required": ["id"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "delete_event",
              "description": "Delete an event by its ID",
              "parameters": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" }
                },
                "required": ["id"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "create_task",
              "description": "Create a new task",
              "parameters": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" },
                  "description": { "type": "string" },
                  "date": { "type": "string", "description": "ISO-8601 date" },
                  "importance": { "type": "string", "enum": ["Low","Normal","High"] },
                  "color": { "type": "string", "description": "Optional color for the event with words" }
                },
                "required": ["title","date"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "update_task",
              "description": "Replace an existing task's details",
              "parameters": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" },
                  "title": { "type": "string" },
                  "description": { "type": "string" },
                  "date": { "type": "string", "description": "ISO-8601 date" },
                  "importance": { "type": "string", "enum": ["Low","Normal","High"] },
                  "is_completed": { "type": "boolean" }
                },
                "required": ["id"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "delete_task",
              "description": "Delete a task by its ID",
              "parameters": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" }
                },
                "required": ["id"]
              }
            }
          },
          {
            "type": "function",
            "function": {
              "name": "internet_search",
              "description": "Perform an internet search and return the top result",
              "parameters": {
                "type": "object",
                "properties": {
                    "query": { "type": "string", "description": "Search query" }
                },
                "required": ["query"]
              }
            }
          }
        ]
        """);
  public async Task<DataMessage> GenerateAssistantMessageAsync(
  Chat chat,
  ApplicationUser user,
  CancellationToken ct = default)
  {
    // 1) build conversation history
    var history = new List<PromptMessage> { new("system", SystemPrompt) };
    var messages = await context.Messages
        .Where(m => m.ChatId == chat.Id)
        .OrderBy(m => m.Pos)
        .ToListAsync(ct);
    history.AddRange(messages.Select(m => new PromptMessage(m.Role.ToString().ToLowerInvariant(), m.Text)));

    DataMessage finalMsg = null!;
    int position = messages.Count;

    // 2) loop until the model returns no function calls
    while (true)
    {
      // send the history with tools enabled every time
      var req = new PromptRequest(
          history,
          Tools: ToolDoc.RootElement,
          ToolChoice: "auto");
      var resp = await router.SendAsync(req, user, ct);

      // if no calls, this is our final assistant output
      if (!resp.HasToolCalls)
      {
        finalMsg = await PersistAssistantReplyAsync(
            chat,
            resp.Content ?? string.Empty,
            position,
            ct);
        break;
      }

      // otherwise, record the assistant’s “tool call” message
      var assistantWithCalls = new PromptMessage(
          "assistant",
          resp.Content ?? string.Empty,
          resp.ToolCalls);
      history.Add(assistantWithCalls);

      // execute each function and append its “tool” output
      foreach (var call in resp.ToolCalls!)
      {
        var payload = await ExecuteToolCallAsync(call, chat.UserId, ct);
        if (payload is null) continue;

        history.Add(new PromptMessage(
            "tool",
            payload,
            ToolCalls: null,
            ToolCallId: call.Id));
      }
      // now loop back, sending the enriched history again
    }

    return finalMsg;
  }

  // ──────────────────────────────────────────────────────────────────────────
  // public async Task<DataMessage> GenerateAssistantMessageAsync(
  //     Chat chat,
  //     ApplicationUser user,
  //     CancellationToken ct = default)
  // {
  //     // 1) build conversation history
  //     var history = new List<PromptMessage> { new("system", SystemPrompt) };
  //
  //     var messages = await context.Messages
  //         .Where(m => m.ChatId == chat.Id)
  //         .OrderBy(m => m.Pos)
  //         .ToListAsync(ct);
  //
  //     history.AddRange(messages.Select(m => new PromptMessage(m.Role.ToString().ToLowerInvariant(), m.Text)));
  //
  //     // 2) first pass – let the model decide whether to call a tool
  //     var firstReq = new PromptRequest(
  //         history,
  //         Tools: ToolDoc.RootElement,
  //         ToolChoice: "auto");
  //
  //     var firstResp = await router.SendAsync(firstReq, user, ct);
  //
  //     // 3) if no tool call, persist answer & return
  //     if (!firstResp.HasToolCalls)
  //         return await PersistAssistantReplyAsync(
  //             chat, firstResp.Content ?? string.Empty, messages.Count, ct);
  //
  //     // 4) add the assistant message that CONTAINS the tool_calls
  //     var assistantWithCalls = new PromptMessage(
  //         "assistant",
  //         firstResp.Content ?? string.Empty,
  //         firstResp.ToolCalls);
  //     history.Add(assistantWithCalls);
  //
  //     // 5) handle each tool-call, append the tool messages
  //     foreach (var call in firstResp.ToolCalls!)
  //     {
  //         var payload = await ExecuteToolCallAsync(call, chat.UserId, ct);
  //         if (payload is null) continue; // unknown tool → skip
  //
  //         history.Add(new PromptMessage(
  //             "tool",
  //             payload,
  //             ToolCalls: null,
  //             ToolCallId: call.Id));
  //     }
  //
  //     // 6) second pass – assistant now has the data
  //     var followUp = new PromptRequest(history);
  //     var finalResp = await router.SendAsync(followUp, user, ct);
  //
  //     return await PersistAssistantReplyAsync(
  //         chat, finalResp.Content ?? string.Empty, messages.Count, ct);
  // }
  //
  private async Task<string?> ExecuteToolCallAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    Console.WriteLine($"\n\n\nAgent executing tool call: {call.Name} with args: {call.Arguments}\n\n\n");
    return call.Name switch
    {
      "get_events_in_time_range" => await HandleGetEventsInTimeRangeAsync(call, userId, ct),
      "get_tasks_in_day_range" => await HandleGetTasksInDayRangeAsync(call, userId, ct),
      "create_event" => await HandleCreateEventAsync(call, userId, ct),
      "update_event" => await HandleUpdateEventAsync(call, userId, ct),
      "delete_event" => await HandleDeleteEventAsync(call, userId, ct),
      "create_task" => await HandleCreateTaskAsync(call, userId, ct),
      "update_task" => await HandleUpdateTaskAsync(call, userId, ct),
      "delete_task" => await HandleDeleteTaskAsync(call, userId, ct),
      "internet_search" => await HandleInternetSearchAsync(call, userId, ct),
      _ => null // unknown tool
    };
  }

  private static JsonElement NormalizeArguments(JsonElement element)
  {
    if (element.ValueKind != JsonValueKind.String)
      return element;

    using var tmp = JsonDocument.Parse(element.GetString() ?? "{}");
    return tmp.RootElement.Clone();
  }

  private async Task<string> HandleGetEventsInTimeRangeAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);

    var start = DateTime.Parse(
        args.GetProperty("start").GetString()!,
        null, DateTimeStyles.RoundtripKind);

    var end = DateTime.Parse(
        args.GetProperty("end").GetString()!,
        null, DateTimeStyles.RoundtripKind);

    // Fetch events via service, then enforce per-user filter (if any)
    var events = await calendarService.GetEventsInTimeRangeAsync(start, end, userId);
    if (!string.IsNullOrEmpty(userId))
      events = events.Where(e => e.UserId == userId).ToList();

    return JsonSerializer.Serialize(new
    {
      events = events.Select(e => new
      {
        e.Id,
        e.Title,
        e.Description,
        start = e.Start,
        end = e.End,
        e.Location,
        e.IsAllDay,
        e.IsInPerson,
        e.MeetingLink,
        e.Color,
        importance = e.Importance.ToString()
      })
    });
  }

  private async Task<string> HandleGetTasksInDayRangeAsync(
    ToolCall call,
    string? userId,
    CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);

    var startDay = DateOnly.FromDateTime(
        DateTime.Parse(args.GetProperty("start_day").GetString()!,
            null, DateTimeStyles.RoundtripKind));

    var endDay = DateOnly.FromDateTime(
        DateTime.Parse(args.GetProperty("end_day").GetString()!,
            null, DateTimeStyles.RoundtripKind));

    List<UserTask> tasks;

    if (!string.IsNullOrEmpty(userId))
    {
      tasks = await taskService.GetTasksInDateRangeAsync(startDay, endDay, userId);
    }
    else
    {
      // fall back to a non-scoped query if no userId is provided
      tasks = await taskService.GetTasksAsync(
          t => t.Date >= startDay && t.Date <= endDay);
    }

    // keep results deterministic
    tasks = tasks
        .OrderBy(t => t.Date)
        .ThenBy(t => t.Title)
        .ToList();

    return JsonSerializer.Serialize(new
    {
      tasks = tasks.Select(t => new
      {
        t.Id,
        t.Title,
        t.Description,
        date = t.Date,
        importance = t.Importance.ToString(),
        t.IsCompleted
      })
    });
  }


  private async Task<string> HandleCreateEventAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);

    // required
    var title = args.GetProperty("title").GetString()!;
    var start = DateTime.Parse(
        args.GetProperty("start").GetString()!,
        null, DateTimeStyles.RoundtripKind);
    var end = DateTime.Parse(
        args.GetProperty("end").GetString()!,
        null, DateTimeStyles.RoundtripKind);

    // optional
    var description = args.TryGetProperty("description", out var descProp)
        ? descProp.GetString()
        : null;

    var location = args.TryGetProperty("location", out var locProp)
        ? locProp.GetString()
        : null;

    var isAllDay = args.TryGetProperty("is_all_day", out var allDayProp)
        && allDayProp.GetBoolean();

    var isInPerson = args.TryGetProperty("is_in_person", out var inPersonProp)
        && inPersonProp.GetBoolean();

    var meetingLink = args.TryGetProperty("meeting_link", out var linkProp)
        ? linkProp.GetString()
        : null;

    var importance = Enum.Parse<Importance>(
        args.TryGetProperty("importance", out var impProp)
            ? impProp.GetString()!
            : "Medium");

    var ev = new Event
    {
      Title = title,
      Description = description,
      Start = start,
      End = end,
      Location = location,
      IsAllDay = isAllDay,
      IsInPerson = isInPerson,
      MeetingLink = meetingLink,
      Importance = importance,
      UserId = userId
    };

    await calendarService.AddEventAsync(ev);
    return JsonSerializer.Serialize(new { success = true, id = ev.Id });
  }
  private async Task<string> HandleUpdateEventAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);
    var id = args.GetProperty("id").GetInt32();

    // 1. Fetch the existing event
    var ev = await calendarService.GetEventByIdAsync(id);
    if (ev == null)
      return JsonSerializer.Serialize(new { success = false, error = "Event not found", id });

    // 2. Only update provided fields
    if (args.TryGetProperty("title", out var titleProp)) ev.Title = titleProp.GetString();
    if (args.TryGetProperty("description", out var descProp)) ev.Description = descProp.GetString();
    if (args.TryGetProperty("start", out var startProp)) ev.Start = DateTime.Parse(startProp.GetString()!, null, DateTimeStyles.RoundtripKind);
    if (args.TryGetProperty("end", out var endProp)) ev.End = DateTime.Parse(endProp.GetString()!, null, DateTimeStyles.RoundtripKind);
    if (args.TryGetProperty("location", out var locProp)) ev.Location = locProp.GetString();
    if (args.TryGetProperty("is_all_day", out var allDayProp)) ev.IsAllDay = allDayProp.GetBoolean();
    if (args.TryGetProperty("is_in_person", out var inPersProp)) ev.IsInPerson = inPersProp.GetBoolean();
    if (args.TryGetProperty("meeting_link", out var linkProp)) ev.MeetingLink = linkProp.GetString();
    if (args.TryGetProperty("importance", out var impProp)) ev.Importance = Enum.Parse<Importance>(impProp.GetString() ?? "Normal");

    // You might want to enforce userId or permissions here

    var ok = await calendarService.ReplaceEventAsync(ev);
    return JsonSerializer.Serialize(new { success = ok, id = ev.Id });
  }

  private async Task<string> HandleDeleteEventAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);
    var id = args.GetProperty("id").GetInt32();
    var ok = await calendarService.DeleteEventAsync(id);
    return JsonSerializer.Serialize(new { success = ok, id });
  }

  private async Task<string> HandleCreateTaskAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);
    Console.WriteLine($"Creating task with args: {args}");
    // {"title":"Walk the dog","date":"2025-07-15"}
    var task = new UserTask
    {
      Title = args.GetProperty("title").GetString()!,
      Description = args.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? string.Empty : string.Empty,
      Date = DateOnly.Parse(args.GetProperty("date").GetString()!),
      Importance = Enum.Parse<Importance>(args.TryGetProperty("importance", out var impProp) ? impProp.GetString() ?? "Medium" : "Medium"),
      UserId = userId
    };

    await taskService.AddTaskAsync(task);
    return JsonSerializer.Serialize(new { success = true, id = task.Id });
  }

  private async Task<string> HandleUpdateTaskAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);
    var id = args.GetProperty("id").GetInt32();

    // 1. Fetch the existing task
    var task = await taskService.GetTaskByIdAsync(id);
    if (task == null)
      return JsonSerializer.Serialize(new { success = false, error = "Task not found", id });

    // 2. Only update provided fields
    if (args.TryGetProperty("title", out var titleProp)) task.Title = titleProp.GetString();
    if (args.TryGetProperty("description", out var descProp)) task.Description = descProp.GetString();
    if (args.TryGetProperty("date", out var dateProp)) task.Date = DateOnly.Parse(dateProp.GetString()!);
    if (args.TryGetProperty("importance", out var impProp)) task.Importance = Enum.Parse<Importance>(impProp.GetString() ?? "Normal");
    if (args.TryGetProperty("is_completed", out var compProp)) task.IsCompleted = compProp.GetBoolean();

    // You might want to enforce userId or permissions here

    var ok = await taskService.ReplaceTaskAsync(task);
    return JsonSerializer.Serialize(new { success = ok, id = task.Id });
  }
  private async Task<string> HandleDeleteTaskAsync(
      ToolCall call,
      string? userId,
      CancellationToken ct)
  {
    var args = NormalizeArguments(call.Arguments);
    var id = args.GetProperty("id").GetInt32();
    var ok = await taskService.DeleteTaskAsync(id);
    return JsonSerializer.Serialize(new { success = ok, id });
  }

  private async Task<DataMessage> PersistAssistantReplyAsync(
      Chat chat,
      string replyText,
      int position,
      CancellationToken ct)
  {
    var msg = new DataMessage
    {
      ChatId = chat.Id,
      Role = MessageRole.Assistant,
      Text = replyText,
      Pos = position,
      SentOn = DateTime.UtcNow
    };

    context.Messages.Add(msg);
    await context.SaveChangesAsync(ct);
    return msg;
  }

  private async Task<string> HandleInternetSearchAsync(
          ToolCall call,
          string? userId,
          CancellationToken ct)
  {
	  Console.WriteLine("\n\n\n\n");
    // 1) Parse the incoming arguments
    var args = NormalizeArguments(call.Arguments);
    var query = args.GetProperty("query").GetString()!;

	//    // 2) Hit your Custom Search + scrape top result
	//    var rawJson = await _googleSearchService.SearchAndScrapeAsync(query);
	// Console.WriteLine($"Raw search result: {rawJson}\n\n\n\n\n");
	//    using var doc = JsonDocument.Parse(rawJson);
	//    var root = doc.RootElement;
	//    // If Google returned an error payload, pass it straight back
	//    if (root.TryGetProperty("error", out _))
	//      return rawJson;
	//
	//    var url = root.GetProperty("url").GetString();
	//    var content = root.GetProperty("content").GetString();


        var rawJson = await _googleSearchService.SearchAndScrapeAsync(query);
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return rawJson;

        var url     = root.GetProperty("url").GetString();
        var content = root.GetProperty("content").GetString() ?? string.Empty;

        // 3) Collapse whitespace and trim length
        //    Remove newlines and sequences of spaces
        content = Regex.Replace(content, "\\s+", " ").Trim();
        //    Limit to first 30,000 characters
        if (content.Length > 30000)
            content = content.Substring(0, 30000);


    // 3) Summarize with a fresh LLM call (no tools)
    var subHistory = new List<PromptMessage>
        {
            new PromptMessage(
                "system",
                $"You are a helpful assistant. The user asked: \"{query}\". " +
                "Please extract the key information from the following webpage text and " +
                "return a concise summary of about 100 words."),
            new PromptMessage(
                "user",
                $"Here is the scraped content:\n\n{content}")
        };

    var subReq = new PromptRequest(subHistory);
    var subResp = await router.SendAsync(subReq, await context.Users.FindAsync(userId), ct);
    var summary = subResp.Content?.Trim() ?? "";

    Console.WriteLine($"Search summary: {summary}\n\n\n\n\n");

    // 4) Return just the bits we care about
    return JsonSerializer.Serialize(new
    {
      query,
      url,
      summary
    });
  }
}

