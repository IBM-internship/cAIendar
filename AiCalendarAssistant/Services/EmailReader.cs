using AiCalendarAssistant.Data.Models;
using PromptingPipeline.Interfaces;
using PromptingPipeline.Models;
using System;


internal sealed class EmailReader : IEmailReader
{
	public Task<Email> GetNextEmailAsync(CancellationToken ct = default)
	{
		// In a real application, this would fetch from a real email source.
		var email = new Email
		{
			SendingUserEmail = "ivan.petrov@ibm.com",
			Title = "Board Meeting Q2",
			Body = "Dear Team,\n\nPlease be reminded of the upcoming board meeting scheduled for Q2. The details are as follows:\n\nDate: 2025-06-15\nStart Time: 10:30 am\nEnd Time: 1:00 pm\n\nBest regards,\nIvan Petrov",
			CreatedOn = DateTime.UtcNow.AddHours(3),
			IsProcessed=false
		};

        return Task.FromResult(email);
    }
}
