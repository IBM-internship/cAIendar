using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using System;

namespace PromptingPipeline.Services;

internal sealed class EmailReader : IEmailReader
{
    public Task<Email> GetNextEmailAsync(CancellationToken ct = default)
    {
        // In a real application, this would fetch from a real email source.
        var email = new Email(
            From: "ivan.petrov@ibm.com",
            Subject: "Board Meeting Q2",
            Body: "Dear Team,\n\nPlease be reminded of the upcoming board meeting scheduled for Q2. The details are as follows:\n\nDate: 2025-06-15\nStart Time: 10:30 am\nEnd Time: 1:00 pm\n\nBest regards,\nIvan Petrov",
			DateReceived: DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd"),
			TimeReceived: TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).ToString("HH:mm"),
			DayOfWeekReceived: DateTime.UtcNow.AddHours(3).DayOfWeek.ToString()
			);

		//print the email layout:
		Console.WriteLine($"From: {email.From}");
		Console.WriteLine($"Subject: {email.Subject}");
		Console.WriteLine($"Body: {email.Body}");
		Console.WriteLine($"Date Received: {email.DateReceived}");
		Console.WriteLine($"Time Received: {email.TimeReceived}");
		Console.WriteLine($"Day of Week Received: {email.DayOfWeekReceived}");

        return Task.FromResult(email);
    }
}
