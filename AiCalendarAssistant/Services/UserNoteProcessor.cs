using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using AiCalendarAssistant.Data.Models;
using System.Text.Json;


internal sealed class UserNoteProcessor
{
    private readonly PromptRouter _router;
    private readonly IUserNoteReader _reader;

    public UserNoteProcessor(PromptRouter router, IUserNoteReader reader)
    {
        _router = router;
        _reader = reader;
    }

    public async Task ProcessUserNoteAsync(CancellationToken ct = default)
    {
        var note = await _reader.GetNextUserNoteAsync(ct);

        using var schemaDoc = JsonDocument.Parse("""
        {
          "type": "json_schema",
          "json_schema": {
            "name": "note_info",
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
              "required": ["title_of_event", "date", "start_time", "importance", "description", "has_end_time"],
              "additionalProperties": false
            }
          }
        }
        """);
		// Granite ignores the "description" field, so put any format in the system prompt.

        var prompt = new PromptRequest(new()
        {
            new("system", "You are an assistant that extracts information from the user's notes and helps organise the user's calendar events. Pay attention to the format required for the response - hours must be in 24 hour format., you must list the importance of the event, and the date must be in YYYY-MM-DD format."),
            new("user",
                $"Extract the imporant parts of this note and format them in the coresponding json so the event can be added into my calendar\n\nTitle: {note.Title}\nBody:\n{note.Body}\nDate Created:{note.CreatedOn.ToString()}")
        },
		// Extra: new(){["temperature"] = 0.7, ["top_p"] = 0.9},
		// Extra: new(){["temperature"] = 1.5, ["top_p"] = 0.8},
        ResponseFormat: schemaDoc.RootElement);

        var response = await _router.SendAsync(prompt, ct);

        Console.WriteLine($"Extracted UserNote Info → {response.Content}");

		// var n = new Event
		// {
		// 	Title = response.GetString("title_of_event") ?? string.Empty,
		// 	Description = response.GetString("description"),
		// 	Start = DateTime.Parse($"{response.GetString("date")}T{response.GetString("start_time")}:00"),
		// 	End = response.GetBool("has_end_time") 
		// 		? DateTime.Parse($"{response.GetString("date")}T{response.GetString("end_time")}:00")
		// 		: DateTime.Parse($"{response.GetString("date")}T{response.GetString("start_time")}:00"),
		// 	IsAllDay = false,
		// 	Color = null,
		// 	Location = null,
		// 	IsInPerson = response.GetBool("is_in_person"),
		// 	MeetingLink = null,
		// 	UserId = null,
		// 	User = null
		// };
	}
}

