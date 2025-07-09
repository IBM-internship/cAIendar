using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

public class GmailEmailService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _ctx;

    public GmailEmailService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHttpContextAccessor ctx)
    {
        this._db = db;
        this._userManager = userManager;
        this._ctx = ctx;
    }
    public async Task<List<Email>> GetLastEmailsAsync()
    {
        var http = _ctx.HttpContext!;

        // Get the currently logged-in user
        var user = await _userManager.GetUserAsync(http.User);
        if (user == null)
            throw new Exception("User is not authenticated.");

        var userId = user.Id; // This is what you need

        var token = await http.GetTokenAsync(GoogleDefaults.AuthenticationScheme, "access_token");
        var cred = GoogleCredential.FromAccessToken(token!)
            .CreateScoped(GmailService.Scope.GmailReadonly);

        var svc = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = cred,
            ApplicationName = "AiCalendarAssistant"
        });

        var listReq = svc.Users.Messages.List("me");
        listReq.MaxResults = 10;
        var listRes = await listReq.ExecuteAsync();

        var emails = new List<Email>();

        if (listRes.Messages is null)
            return emails;

        foreach (var msg in listRes.Messages)
        {
            // Check if email already exists in the DB
            if (await _db.Emails.AnyAsync(e => e.GmailMessageId == msg.Id))
            {
                // Already in DB, retrieve it to return for display
                var existing = await _db.Emails.FirstAsync(e => e.GmailMessageId == msg.Id);
                emails.Add(existing);
                continue;
            }

            // Not in DB, fetch full message
            var getReq = svc.Users.Messages.Get("me", msg.Id);
            getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            var detail = await getReq.ExecuteAsync();

            var headers = detail.Payload.Headers!;
            var dateStr = headers.FirstOrDefault(h => h.Name == "Date")?.Value;
            DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);

            var body = await GetEmailBodyAsync(detail);

            var email = new Email
            {
                GmailMessageId = msg.Id, // Id of the Gmail message from Google API
                CreatedOn = date, // Date from Google API
                SendingUserEmail = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "", // Sender from Google API
                Title = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "", // Subject from Google API
                Body = body,
                RecievingUserId = userId, // Id of the currently logged-in user
            };

            _db.Emails.Add(email);
            emails.Add(email);
        }

        await _db.SaveChangesAsync();
        return emails;
    }

    private async Task<string> GetEmailBodyAsync(Google.Apis.Gmail.v1.Data.Message message)
    {
        return await Task.Run(() =>
        {
            if (message.Payload?.Body?.Data != null)
                return DecodeBase64Url(message.Payload.Body.Data);

            var plainPart = message.Payload?.Parts?.FirstOrDefault(p => p.MimeType == "text/plain");
            if (plainPart?.Body?.Data != null)
                return DecodeBase64Url(plainPart.Body.Data);

            var htmlPart = message.Payload?.Parts?.FirstOrDefault(p => p.MimeType == "text/html");
            if (htmlPart?.Body?.Data != null)
                return DecodeBase64Url(htmlPart.Body.Data);

            return "(No message content)";
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
}
