using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface IGmailEmailService
{
    Task<List<Email>> GetLastEmailsAsync();
    Task<bool> ReplyToEmailAsync(
        string? messageId,
        string originalSubject,
        string fromEmail,
        string body,
        string userId);
}