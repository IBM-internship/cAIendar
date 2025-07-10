using System.Text.Json;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using Message = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public class EmailProcessor(PromptRouter router, EventProcessor eventProcessor)
{
    private static readonly JsonDocument IsRelevantEventSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "is_relevant_event",
            "strict": true,
            "schema": {
              "type": "object",
              "properties": {
                "is_event": { "type": "boolean" }
              },
              "required": ["is_relevant_event"],
              "additionalProperties": false
            }
          }
        }
        """); 
    
    private static readonly JsonDocument EmailInfoSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "email_info",
            "strict": true,
            "schema": {
              "type": "object",
              "properties": {
                "title_of_event": { "type": "string" },
                "importance": { "type": "string", "enum": ["high", "medium", "low"]},
                "date": { "type": "string" },
                "start_time": { "type": "string" },
                "end_time": { "type": "string" },
                "description": { "type": "string" },
                "is_in_person": { "type": "boolean" },
                "has_end_time": { "type": "boolean" },
                "is_all_day": { "type": "boolean" },
                "location": { "type": "string" }
              },
              "required": ["title_of_event", "date", "start_time", "end_time", "importance", "description", "has_end_time"],
              "additionalProperties": false
            }
          }
        }
        """);

    public async Task ProcessEmailAsync(ApplicationUser user, Email email, CancellationToken ct = default)
    {
        if (email.IsProcessed) return;
        email.IsProcessed = true;
        
        var isRelevant = await IsRelevantEventAsync(email, ct);
        if (!isRelevant)  return;
        
        var calendarEvent = await ExtractEventAsync(user, email, ct);
        await eventProcessor.ProcessEventAsync(calendarEvent, ct);
    }

    private async Task<bool> IsRelevantEventAsync(Email email, CancellationToken ct = default)
    {
        var prompt = new PromptRequest([
                new Message("system",
                    """
                    You are an assistant that determines if an email is relevant for creating a calendar event. 
                    Pay attention to the format required for the response.
                    """),
                new Message("user",
                    $"""
                     Determine if this email is relevant for creating a calendar event:
                     
                     From: {email.SendingUserEmail}
                     Subject: {email.Title}
                     Body:
                     {email.Body}
                     """)
            ],
            ResponseFormat: IsRelevantEventSchema.RootElement);

        var response = await router.SendAsync(prompt, ct);

        using var doc = JsonDocument.Parse(response.Content!);
        return doc.RootElement.GetProperty("is_relevant_event").GetBoolean();
    }
    
    private async Task<Event> ExtractEventAsync(ApplicationUser user, Email email, CancellationToken ct = default)
    {
        var prompt = new PromptRequest([
                new Message("system",
                    """
                    You are an assistant that extracts information from emails and helps organise the user's calendar events. 
                    Pay attention to the format required for the response - hours must be in 24 hour format.
                    You must list the importance of the event, and the date must be in YYYY-MM-DD format.
                    """),
                new Message("user",
                    $"""
                     Extract the important parts of the email and format them in the corresponding json so the event can be added into my calendar
                     
                     From: {email.SendingUserEmail}
                     Subject: {email.Title}
                     Body:
                     {email.Body}
                     """)
            ],
            ResponseFormat: EmailInfoSchema.RootElement);

        var response = await router.SendAsync(prompt, ct);

        using var eventDoc = JsonDocument.Parse(response.Content!);

        var root = eventDoc.RootElement;

        var calendarEvent = new Event
        {
            Title = root.GetProperty("title_of_event").GetString() ?? "",
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Start = DateTime.Parse(
                $"{root.GetProperty("date").GetString()} {root.GetProperty("start_time").GetString()}"),
            End = root.GetProperty("has_end_time").GetBoolean()
                ? DateTime.Parse($"{root.GetProperty("date").GetString()} {root.GetProperty("end_time").GetString()}")
                : DateTime.Parse($"{root.GetProperty("date").GetString()} 23:59:59"),
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
            User = user
        };

        return calendarEvent;
    }
}