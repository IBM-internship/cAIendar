namespace PromptingPipeline.Models;

public sealed record UserNote(
    string Title,
    string Body,
	string DateCreated,
	string TimeCreated,
	stirng IsProcessed = "false",
	string DayOfWeek); // again not important/dont keep in the db
