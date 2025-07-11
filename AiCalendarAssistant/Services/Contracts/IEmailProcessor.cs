using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface IEmailProcessor
{
    Task ProcessEmailAsync(ApplicationUser user, Email email, CancellationToken ct = default);
}