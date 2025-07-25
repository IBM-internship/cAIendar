﻿using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using AiCalendarAssistant.Services.Contracts;
using Email = AiCalendarAssistant.Data.Models.Email;
using Message = Google.Apis.Gmail.v1.Data.Message;
using static AiCalendarAssistant.Utilities.TaskExtensions;

namespace AiCalendarAssistant.Services;

public class GmailEmailService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor ctx,
    TokenRefreshService tokenService,
    ILogger<GmailEmailService> logger,
    IServiceProvider serviceProvider,
    IServiceScopeFactory serviceScopeFactory) : IGmailEmailService
{
    public async Task<List<Email>> GetLastEmailsAsync()
    {
        var http = ctx.HttpContext!;

        var user = await userManager.GetUserAsync(http.User);
        if (user == null)
            throw new Exception("User is not authenticated.");

        var userId = user.Id;

        var service = await GetGmailServiceAsync(userId);
        if (service == null)
            throw new UnauthorizedAccessException("Valid access token not available");

        var listReq = service.Users.Messages.List("me");
        listReq.MaxResults = 10;
        listReq.Q = "in:inbox -in:sent";
        var listRes = await listReq.ExecuteAsync();

        var emails = new List<Email>();

        if (listRes.Messages is null)
            return emails;

        foreach (var msg in listRes.Messages)
        {
            if (await db.Emails.AnyAsync(e => e.GmailMessageId == msg.Id))
            {
                var existing = await db.Emails.FirstAsync(e => e.GmailMessageId == msg.Id);
                emails.Add(existing);
                continue;
            }

            var getReq = service.Users.Messages.Get("me", msg.Id!);
            getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            var detail = await getReq.ExecuteAsync();

            var headers = detail.Payload?.Headers!;
            var dateStr = headers.FirstOrDefault(h => h.Name == "Date")?.Value;
            DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

            var body = await GetEmailBodyAsync(detail);

            var email = new Email
            {
                GmailMessageId = msg.Id!,
                CreatedOn = date,
                SendingUserEmail = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "",
                Title = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "",
                Body = body,
                RecievingUserId = userId,
                ThreadId = detail.ThreadId ?? "",
                MessageId = headers.FirstOrDefault(h => h.Name == "Message-ID")?.Value ?? "",
            };

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Adding email {email.GmailMessageId} to the database for user {userId}");
            Console.ResetColor();

            db.Emails.Add(email);
            emails.Add(email);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Processing email {email.GmailMessageId}");
            Console.ResetColor();
            SendAsyncFunc(async () =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var scopedEmailProcessor = scope.ServiceProvider.GetRequiredService<IEmailProcessor>();
                await scopedEmailProcessor.ProcessEmailAsync(user, email);
            }, serviceScopeFactory);
        }

        await db.SaveChangesAsync();
        return emails;
    }

    public async Task<bool> ReplyToEmailAsync(string messageId, string originalSubject,
        string fromEmail, string body, string userId)
    {
        try
        {
            var service = await GetGmailServiceAsync(userId);
            if (service == null)
                return false;

            var profile = await service.Users.GetProfile("me").ExecuteAsync();
            var userEmail = profile.EmailAddress;

            var replySubject = originalSubject.StartsWith("Re: ") ? originalSubject : $"Re: {originalSubject}";

            var replyMessage = CreateReplyMessage(userEmail, fromEmail, replySubject, messageId, body);

            var request = service.Users.Messages.Send(replyMessage, "me");
            await request.ExecuteAsync();

            logger.LogInformation("Reply sent successfully to {FromEmail}", fromEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending reply to {FromEmail}", fromEmail);
            return false;
        }
    }

    private Message CreateReplyMessage(
        string userEmail, 
        string toEmail, 
        string subject, 
        string? inReplyToMessageId,
        string body)
    {
        var emailContent = new StringBuilder();
        emailContent.AppendLine($"From: {userEmail}");
        emailContent.AppendLine($"To: {toEmail}");
        emailContent.AppendLine($"Subject: {subject}");
        if (inReplyToMessageId != null) emailContent.AppendLine($"In-Reply-To: {inReplyToMessageId}");
        emailContent.AppendLine("Content-Type: text/plain; charset=utf-8");
        emailContent.AppendLine();
        emailContent.AppendLine(body);

        var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailContent.ToString()))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");

        return new Message { Raw = rawMessage };
    }

    private async Task<string> GetEmailBodyAsync(Message message)
    {
        return await Task.Run(() =>
        {
            if (message.Payload?.Body?.Data != null)
                return DecodeBase64Url(message.Payload.Body.Data);

            var plainPart = message.Payload?.Parts?.FirstOrDefault(p => p.MimeType == "text/plain");
            if (plainPart?.Body?.Data != null)
                return DecodeBase64Url(plainPart.Body.Data);

            var htmlPart = message.Payload?.Parts?.FirstOrDefault(p => p.MimeType == "text/html");
            return htmlPart?.Body?.Data != null ? DecodeBase64Url(htmlPart.Body.Data) : "(No message content)";
        });
    }

    private string DecodeBase64Url(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }

        var bytes = Convert.FromBase64String(input);
        return Encoding.UTF8.GetString(bytes);
    }

    private async Task<GmailService?> GetGmailServiceAsync(string userId)
    {
        var token = await tokenService.GetValidAccessTokenAsync(userId);

        if (string.IsNullOrEmpty(token))
            return null;

        var cred = GoogleCredential.FromAccessToken(token)
            .CreateScoped(GmailService.Scope.GmailModify);

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = cred,
            ApplicationName = "AiCalendarAssistant"
        });
    }
}