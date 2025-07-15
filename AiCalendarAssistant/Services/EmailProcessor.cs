using System.Text.Json;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services.Contracts;
using Message = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public class EmailProcessor(
    PromptRouter router,
    EventProcessor eventProcessor,
    ITaskService taskService) : IEmailProcessor
{
    private static readonly JsonDocument IsEventSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "is_task_or_event",
            "strict": true,
            "schema": {
              "type": "object",
              "properties": {
                "is_task_or_event": { "type": "boolean" }
              },
              "required": ["is_task_or_event"],
              "additionalProperties": false
            }
          }
        }
        """);

    private static readonly JsonDocument TaskOrEventSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "task_or_event",
            "strict": true,
            "schema": {
              "type": "object",
              "properties": {
                "task_or_event": { "type": "string", "enum": ["task", "event"]}
              },
              "required": ["task_or_event"],
              "additionalProperties": false
            }
          }
        }
        """);

    private static readonly JsonDocument EventInfoSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "email_info",
            "strict": false,
            "schema": {
              "type": "object",
              "properties": {
                "title_of_event": { "type": "string" },
                "importance": { "type": "string", "enum": ["low", "medium", "high"]},
                "date": { "type": "string", "format": "date" },
                "start_time": { "type": "string", "format": "time" },
                "end_time": { "type": "string", "format": "time" },
                "description": { "type": "string" },
                "is_in_person": { "type": "boolean" },
                "has_end_time": { "type": "boolean" },
                "is_all_day": { "type": "boolean" },
                "location": { "type": "string" }
              },
              "required": ["title_of_event", "date", "start_time", "end_time", "is_in_person", "importance", "description"],
              "additionalProperties": false
            }
          }
        }
        """);

    private static readonly JsonDocument TaskInfoSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "email_info",
            "strict": true,
            "schema": {
              "type": "object",
              "properties": {
                "title_of_task": { "type": "string" },
                "date": { "type": "string", "format": "date" },
                "importance": { "type": "string", "enum": ["low", "medium", "high"]},
                "description": { "type": "string" }
              },
              "required": ["title_of_task", "date", "importance", "description"],
              "additionalProperties": false
            }
          }
        }
        """);


    public async Task ProcessEmailAsync(ApplicationUser user, Email email, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Processing email {email.Body}");
        Console.ResetColor();

        if (email.IsProcessed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Email already processed, skipping. {email.Body}");
            Console.ResetColor();
            return;
        }

        email.IsProcessed = true;

        var isRelevant = await IsRelevantEventAsync(email, user, ct);
        if (!isRelevant)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Email is not relevant for calendar event, skipping. {email.Body}");
            Console.ResetColor();
            return;
        }

        var isEvent = await IsEventAsync(email, user, ct);

        if (isEvent)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Email is an event, processing it.");
            Console.ResetColor();

            var calendarEvent = await ExtractEventAsync(user, email, ct);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(calendarEvent);
            Console.ResetColor();

            await eventProcessor.ProcessEventAsync(calendarEvent, user, ct);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Email is a task, processing it.");
            Console.ResetColor();

            var task = await ExtractTaskAsync(user, email, ct);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(task);
            Console.ResetColor();

            await taskService.AddTaskAsync(task);
        }
    }


    private async Task<bool> IsRelevantEventAsync(Email email, ApplicationUser user, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Determining if email is relevant for calendar event. {email.Body}");
        Console.ResetColor();
        var prompt = new PromptRequest([
                // new Message("system",
                //     """
                //     You are an assistant that determines if an email is to create a task or an event.
                //     A task is something that can be done at any time of the day, while an event is something that has a specific time and date.
                //     An event can also be a meeting, an appointment, or any other event that requires scheduling.
                //     A task that has a specific date, but not a specific time to start and end, and could be added to a todo list or a task manager.
                //     """),
				new Message("system",
					"""
					You are a small router that reads emails from the user's inbox and determines if they are relevant to the user and should be added to the user's calendar and or todo list.
					"""),
                new Message("user",
                    $"""
                     Determine if this email is event or task that could be added to the user's calendar or todo list:

                     From: {email.SendingUserEmail}
                     Subject: {email.Title}
                     Body:
                     {email.Body}
                     """)
            ],
            ResponseFormat: IsEventSchema.RootElement);


        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sending prompt to determine relevance...: {prompt}");
        Console.ResetColor();
        // get receiving user with id from email
        var response = await router.SendAsync(prompt, user, ct);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Finished sending prompt to determine relevance: {response.Content}");
        Console.ResetColor();

        using var doc = JsonDocument.Parse(response.Content!);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"IsRelevantEvent: {doc.RootElement.GetProperty("is_task_or_event").GetBoolean()}");
        Console.ResetColor();
        return doc.RootElement.GetProperty("is_task_or_event").GetBoolean();
    }

    private async Task<bool> IsEventAsync(Email email, ApplicationUser user, CancellationToken ct = default)
    {
        var prompt = new PromptRequest([
                // new Message("system",
                //     """
                //     You are an assistant that determines if an email should be processed as a task or as an event.
                //     An event is something that has a specific time and date, while a task is something that can be done at any time of the day.
                //     An event can also be a meeting, an appointment, or any other event that requires scheduling.
                //     A task on the other hand, is something that can be done at any time of the day, and does not have a specific time to start and end.
                //     """),
				new Message("system",
					"""
					Determine if this email should be added to the user's calendar as an event which has a specific time frame and requires attendance, or it should be added to the user's todo list where not so important tasks are stored in a done-undone style todo.
					If the email mentiones a specific time and date, it should be added to the user's calendar as an event.
					"""),
                new Message("user",
                    $"""
                     Determine if this email should be processed as a task or as an event:

                     From: {email.SendingUserEmail}
                     Subject: {email.Title}
                     Body:
                     {email.Body}
                     """)
            ],
            ResponseFormat: TaskOrEventSchema.RootElement);

        var response = await router.SendAsync(prompt, user, ct);
        return JsonDocument.Parse(response.Content!).RootElement.GetProperty("task_or_event").GetString() == "event";
    }


    private async Task<Event> ExtractEventAsync(ApplicationUser user, Email email, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Extracting event from email. {email.Body}");
        Console.ResetColor();
        
        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
        var currentLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
        
        var prompt = new PromptRequest([
            new Message("system",
                $"""
                 You are an assistant that extracts information from emails and helps organise the user's calendar events. 
                 Pay attention to the format required for the response - hours must be in 24 hour format.
                 The user's current local time is: {currentLocalTime:yyyy-MM-dd HH:mm:ss}
                 When extracting dates and times, consider them in the user's local timezone.
                 You must list the importance of the event, and the date must be in YYYY-MM-DD format.
                 """),
            new Message("user",
                $"""
                 Extract the important parts of the email and format them in the corresponding json so the event can be added into my calendar

                 From: {email.SendingUserEmail}
                 Subject: {email.Title}
                 Body:
                 {email.Body}
                 """)],
            ResponseFormat: EventInfoSchema.RootElement);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sending prompt to extract event: {prompt}");
        Console.ResetColor();

        var response = await router.SendAsync(prompt, user, ct);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Finished sending prompt to extract event: {response.Content}");
        Console.ResetColor();

        var root = JsonDocument.Parse(response.Content!).RootElement;
        
        var localStartDateTime = DateTime.Parse($"{root.GetProperty("date").GetString()} {root.GetProperty("start_time").GetString()}");
        var localEndDateTime = root.GetProperty("has_end_time").GetBoolean()
            ? DateTime.Parse($"{root.GetProperty("date").GetString()} {root.GetProperty("end_time").GetString()}")
            : DateTime.Parse($"{root.GetProperty("date").GetString()} 23:59:59");
        
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStartDateTime, userTimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEndDateTime, userTimeZone);

        var calendarEvent = new Event
        {
            Title = root.GetProperty("title_of_event").GetString() ?? "",
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Start = utcStart,
            End = utcEnd,
            IsAllDay = root.TryGetProperty("is_all_day", out var isAllDay) && isAllDay.GetBoolean(),
            IsInPerson = root.TryGetProperty("is_in_person", out var inPerson) && inPerson.GetBoolean(),
            Location = root.TryGetProperty("location", out var location) ? location.GetString() : null,
            Importance = root.GetProperty("importance").GetString() switch
            {
                "high" => Importance.High,
                "medium" => Importance.Medium,
                "low" => Importance.Low,
                _ => Importance.Medium
            },
            Color = root.GetProperty("importance").GetString() switch
            {
                "high" => "red",
                "medium" => "blue",
                "low" => "green",
                _ => "blue"
            },
            MeetingLink = null,
            UserId = user.Id,
            User = user,
            EventCreatedFromEmailId = email.Id,
            EventCreatedFromEmail = email
        };

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            $"""
             Extracted event: {calendarEvent.Title}
             {calendarEvent.Description}
             Start: {calendarEvent.Start}
             End: {calendarEvent.End}
             AllDay: {calendarEvent.IsAllDay}
             Importance: {calendarEvent.Importance}
             """);
        Console.ResetColor();

        return calendarEvent;
    }

    private async Task<UserTask> ExtractTaskAsync(ApplicationUser user, Email email, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Extracting task from email. {email.Body}");
        Console.ResetColor();
        var prompt = new PromptRequest([
                new Message("system",
                    """
                    You are an assistant that extracts information from emails and helps organise the user's calendar events. 
                    Pay attention to the format required for the response - hours must be in 24 hour format.
                    You must list the importance of the event, and the date must be in YYYY-MM-DD format.
                    """),
                new Message("user",
                    $"""
                     Extract the important parts of the email and format them in the corresponding json so the task can be added into my task manager

                     From: {email.SendingUserEmail}
                     Subject: {email.Title}
                     Body:
                     {email.Body}
                     """)
            ],
            ResponseFormat: TaskInfoSchema.RootElement);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sending prompt to extract task: {prompt}");
        Console.ResetColor();

        var response = await router.SendAsync(prompt, user, ct);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Finished sending prompt to extract task: {response.Content}");
        Console.ResetColor();

        var root = JsonDocument.Parse(response.Content!).RootElement;

        var task = new UserTask
        {
            Title = root.GetProperty("title_of_task").GetString() ?? "",
            Description = root.GetProperty("description").GetString() ?? "",
            Date = DateOnly.Parse(root.GetProperty("date").GetString() ?? ""),
            Importance = root.GetProperty("importance").GetString() switch
            {
                "high" => Importance.High,
                "medium" => Importance.Medium,
                "low" => Importance.Low,
                _ => Importance.Medium
            },
            Color = root.GetProperty("importance").GetString() switch
            {
                "high" => "red",
                "medium" => "blue",
                "low" => "green",
                _ => "blue"
            },
            UserId = user.Id
        };

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            $"""
             Extracted task: {task.Title}
             {task.Description}
             Date: {task.Date}
             Importance: {task.Importance}
             """);
        Console.ResetColor();
        return task;
    }
}
