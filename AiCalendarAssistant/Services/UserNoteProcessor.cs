using System.Text.Json;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services.Contracts;

namespace AiCalendarAssistant.Services;

public class UserNoteProcessor(PromptRouter router, IUserNoteReader reader)
{
	public async Task<Event> ProcessUserNoteAsync(CancellationToken ct = default)
    {
        var note = await reader.GetNextUserNoteAsync(ct);

        using var schemaDoc = JsonDocument.Parse("""
        {
          "type": "json_schema",
          "json_schema": {
            "name": "note_info",
            "strict": false,
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
              "required": ["title_of_event", "date", "start_time", "importance", "description", "has_end_time"],
              "additionalProperties": false
            }
          }
        }
        """);
		// Granite ignores the "description" field, so put any format in the system prompt.

        var prompt = new PromptRequest([
		        new("system",
			        "You are an assistant that extracts information from the user's notes and helps organise the user's calendar events. Pay attention to the format required for the response - hours must be in 24 hour format., you must list the importance of the event, and the date must be in YYYY-MM-DD format."),
		        new("user",
			        $"Extract the imporant parts of this note and format them in the coresponding json so the event can be added into my calendar\n\nTitle: {note.Title}\nBody:\n{note.Body}\nDate Created:{note.CreatedOn.ToString()}")
	        ],
		// Extra: new(){["temperature"] = 0.7, ["top_p"] = 0.9},
		// Extra: new(){["temperature"] = 1.5, ["top_p"] = 0.8},
        ResponseFormat: schemaDoc.RootElement);

        var response = await router.SendAsync(prompt, ct);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Extracted UserNote Info → {response.Content}");
		Console.ResetColor();
        
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

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Extracted Email Info → {response.Content}");
		Console.ResetColor();
        
		return calendarEvent;
	}
}

