namespace PromptingPipeline.Models;

public sealed record Email(
    string From,
    string Subject, // title
    string Body,
	string DateReceived,
	string TimeReceived,
	string DayOfWeekReceived, // not important/dont keep in the db
	string IsProcessed = "false");
