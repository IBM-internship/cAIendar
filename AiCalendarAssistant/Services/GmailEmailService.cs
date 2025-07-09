using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using System.Globalization;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Services
{
    public class GmailEmailService(IHttpContextAccessor ctx, TokenRefreshService tokenService)
    {
        public async Task<List<Email>> GetLastEmailsAsync()
        {
            var token = await tokenService.GetValidAccessTokenAsync();
        
            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Valid access token not available");
            }

            var cred = GoogleCredential.FromAccessToken(token)
                .CreateScoped(GmailService.Scope.GmailReadonly);

            var svc = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName      = "AiCalendarAssistant"
            });

            var listReq = svc.Users.Messages.List("me");
            listReq.MaxResults = 10;
            var listRes = await listReq.ExecuteAsync();

            var emails = new List<Email>();
            if (listRes.Messages is null)
                return emails;

            foreach (var msg in listRes.Messages)
            {
                var getReq = svc.Users.Messages.Get("me", msg.Id!);
                getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var detail = await getReq.ExecuteAsync();

                var hdrs = detail.Payload?.Headers!;
                var dateStr = hdrs.FirstOrDefault(h => h.Name == "Date")?.Value;
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

                emails.Add(new Email
                {
                    Date    = date,
                    From    = hdrs.FirstOrDefault(h => h.Name == "From")?.Value ?? "",
                    Subject = hdrs.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "",
                    Snippet = detail.Snippet ?? ""
                });
            }

            return emails;
        }
    }
}