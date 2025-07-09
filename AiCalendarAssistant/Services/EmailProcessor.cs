using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using System.Text.Json;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;

internal sealed class EmailProcessor
{
    private readonly PromptRouter _router;
    private readonly IEmailReader _reader;
    private readonly ICalendarService _calendar;     

    public EmailProcessor(
        PromptRouter router,
        IEmailReader reader,
        ICalendarService calendar)                   
    {
        _router   = router;
        _reader   = reader;
        _calendar = calendar;
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
		// Granite ignores the "description" field, so put any format in the system prompt.

        var prompt = new PromptRequest(new()
        {
            new("system", "You are an assistant that extracts information from emails and helps organise the user's calendar events. Pay attention to the format required for the response - hours must be in 24 hour format., you must list the importance of the event, and the date must be in YYYY-MM-DD format."),
            new("user",
                $"Extract the imporant parts of the email and format them in the coresponding json so the event can be added into my calendar\n\nFrom: {email.SendingUserEmail}\nSubject: {email.Title}\nBody:\n{email.Body}")
        },
        ResponseFormat: schemaDoc.RootElement);

        var response = await _router.SendAsync(prompt, ct);

        // Parse the response content into a JsonDocument
        using var eventDoc = JsonDocument.Parse(response.Content);

        var root = eventDoc.RootElement;

        // Map the extracted fields to the Event model
        var calendarEvent = new Event
        {
            Title = root.GetProperty("title_of_event").GetString() ?? "",
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Start = DateTime.Parse($"{root.GetProperty("date").GetString()} {root.GetProperty("start_time").GetString()}"),
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
			UserId = null, // FIX THIS!
			User = null, // maybe this also idk what it does
		};

        Console.WriteLine($"Extracted Email Info → {response.Content}");

		await _calendar.AddEventAsync(calendarEvent);   
		Console.WriteLine($"Event #{calendarEvent.Id} saved!");

	}
}

