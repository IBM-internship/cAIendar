using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Globalization;
using System.Text;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Services
{
    public class GmailEmailService(
        IHttpContextAccessor ctx,
        TokenRefreshService tokenService,
        ILogger<GmailEmailService> logger)
    {
        public async Task<List<Email>> GetLastEmailsAsync()
        {
            var service = await GetGmailServiceAsync();
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
                var getReq = service.Users.Messages.Get("me", msg.Id!);
                getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var detail = await getReq.ExecuteAsync();

                var hdrs = detail.Payload?.Headers!;
                var dateStr = hdrs.FirstOrDefault(h => h.Name == "Date")?.Value;
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

                emails.Add(new Email
                {
                    Id = msg.Id!,
                    Date = date,
                    From = hdrs.FirstOrDefault(h => h.Name == "From")?.Value ?? "",
                    Subject = hdrs.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "",
                    Snippet = detail.Snippet ?? "",
                    ThreadId = detail.ThreadId ?? "",
                    MessageId = hdrs.FirstOrDefault(h => h.Name == "Message-ID")?.Value ?? ""
                });
            }

            return emails;
        }

        public async Task<bool> ReplyToEmailAsync(string messageId, string threadId, string originalSubject,
            string fromEmail)
        {
            try
            {
                var service = await GetGmailServiceAsync();
                if (service == null)
                    return false;

                var profile = await service.Users.GetProfile("me").ExecuteAsync();
                var userEmail = profile.EmailAddress;

                var replySubject = originalSubject.StartsWith("Re: ") ? originalSubject : $"Re: {originalSubject}";

                var replyMessage = CreateReplyMessage(userEmail, fromEmail, replySubject, messageId);

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

        private Message CreateReplyMessage(string userEmail, string toEmail, string subject, string inReplyToMessageId)
        {
            const string body = "this is a test reply\nfrom the user's email";

            var emailContent = new StringBuilder();
            emailContent.AppendLine($"From: {userEmail}");
            emailContent.AppendLine($"To: {toEmail}");
            emailContent.AppendLine($"Subject: {subject}");
            emailContent.AppendLine($"In-Reply-To: {inReplyToMessageId}");
            emailContent.AppendLine("Content-Type: text/plain; charset=utf-8");
            emailContent.AppendLine();
            emailContent.AppendLine(body);

            var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailContent.ToString()))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            return new Message { Raw = rawMessage };
        }

        private async Task<GmailService?> GetGmailServiceAsync()
        {
            var token = await tokenService.GetValidAccessTokenAsync();

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
}