using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using PromptingPipeline.Services;
using System.Text.Json;
using AiCalendarAssistant.Data.Models;
namespace PromptingPipeline.Services;

internal sealed class EmailProcessor
{
    private readonly PromptRouter _router;
    private readonly IEmailReader _reader;

    public EmailProcessor(PromptRouter router, IEmailReader reader)
    {
        _router = router;
        _reader = reader;
    }

    public async Task ProcessEmailAsync(CancellationToken ct = default)
    {
        var email = await _reader.GetNextEmailAsync(ct);

        using var schemaDoc = JsonDocument.Parse("""
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
                "has_end_time": { "type": "boolean" }
              },
              "required": ["title_of_event", "date", "start_time", "end_time", "importance", "description", "has_end_time"],
              "additionalProperties": false
            }
          }
        }
        """);
		// Granite ignores the "description" field, so put any format in the system prompt.

        var prompt = new PromptRequest(new()
        {
            new("system", "You are an assistant that extracts information from emails and helps organise the user's calendar events. Pay attention to the format required for the response - hours must be in 24 hour format., you must list the importance of the event, and the date must be in YYYY-MM-DD format."),
            new("user",
                $"Extract the imporant parts of the email and format them in the coresponding json so the event can be added into my calendar\n\nFrom: {email.SendingUserEmail}\nSubject: {email.Title}\nBody:\n{email.Body}")
        },
        ResponseFormat: schemaDoc.RootElement);

        var response = await _router.SendAsync(prompt, ct);

        Console.WriteLine($"Extracted Email Info → {response.Content}");

        // var e = new Event
        // {
        //     Title = response.GetString("title_of_event"),
        //     Description = response.GetString("description"),
        //     Start = DateTime.Parse($"{response.GetString("date")}T{response.GetString("start_time")}"),
        //     End = response.GetBoolean("has_end_time")
        //         ? DateTime.Parse($"{response.GetString("date")}T{response.GetString("end_time")}")
        //         : DateTime.Parse($"{response.GetString("date")}T{response.GetString("start_time")}"),
        //     IsAllDay = false,
        //     Color = null,
        //     Location = null,
        //     IsInPerson = response.GetBoolean("is_in_person"),
        //     MeetingLink = null,
        //     UserId = null,
        //     User = null
        // };
		//
		// Console.WriteLine($"Event Created: {e.Title} on {e.Date.ToShortDateString()} from {e.StartTime} to {e.EndTime}");
		//
		// email.IsProcessed = true;
		//
		//
		// await ICalendarService.AddEventAsync(e);
    }
}

