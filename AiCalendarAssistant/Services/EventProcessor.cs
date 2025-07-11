using System.Text;
using System.Text.Json;
using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using static AiCalendarAssistant.Utilities.TaskExtensions;
using Message = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public class EventProcessor(
    ApplicationDbContext db,
    PromptRouter router,
    IServiceScopeFactory serviceScopeFactory)
{
    private static readonly JsonDocument ShouldReplaceEventSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "email_info",
            "strict": false,
            "schema": {
              "type": "object",
              "properties": {
                "should-change-event": { "type": "boolean" }
              },
              "required": ["should-change-event"],
              "additionalProperties": false
            }
          }
        }
        """);

    public async Task ProcessEventAsync(Event newEvent, CancellationToken ct = default)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Processing event {newEvent.Title}");
        Console.ResetColor();

        if (newEvent.User != null)
        {
            var existingUser = await db.Users.FindAsync([newEvent.User.Id], cancellationToken: ct);
            if (existingUser != null)
            {
                newEvent.User = existingUser;
            }
            else
            {
                db.Entry(newEvent.User).State = EntityState.Detached;
            }
        }

        if (newEvent.EventCreatedFromEmailId.HasValue)
        {
            var existingEmail = await db.Emails.FindAsync([newEvent.EventCreatedFromEmailId.Value], cancellationToken: ct);
            if (existingEmail != null)
            {
                newEvent.EventCreatedFromEmail = existingEmail;
            }
        }

        var collidingEvents = await GetCollidingEventsAsync(newEvent, ct);

        if (collidingEvents.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No colliding events found, adding new event.");
            Console.ResetColor();

            db.Events.Add(newEvent);
            await db.SaveChangesAsync(ct);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("There are colliding events, checking their importance and whether to replace them.");
        Console.ResetColor();

        var mostImportantEvent = collidingEvents
            .OrderByDescending(e => e.Importance)
            .FirstOrDefault();

        if (mostImportantEvent == null)
        {
            throw new Exception("This should never happen, there should always be at least one colliding event.");
        }

        var sameCategory = mostImportantEvent.Importance == newEvent.Importance;
        var shouldReplace = sameCategory && ShouldReplaceEvent(collidingEvents, newEvent);

        if ((sameCategory && !shouldReplace) || mostImportantEvent.Importance > newEvent.Importance)
        {
            SendAsyncFunc(SendCancellationEmailWithScopeAsync(
                newEvent.EventCreatedFromEmail!.SendingUserEmail,
                mostImportantEvent,
                newEvent.EventCreatedFromEmail!));
        }
        else if ((sameCategory && shouldReplace) || mostImportantEvent.Importance < newEvent.Importance)
        {
            foreach (var collidingEvent in collidingEvents)
            {
                db.Events.Remove(collidingEvent);

                SendAsyncFunc(SendCancellationEmailWithScopeAsync(
                    collidingEvent.EventCreatedFromEmail!.SendingUserEmail,
                    newEvent,
                    collidingEvent.EventCreatedFromEmail!));
            }

            db.Events.Add(newEvent);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SendCancellationEmailWithScopeAsync(string recipient, Event cancelledEvent,
        Email reasonForCancellation)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var scopedEmailComposer = scope.ServiceProvider.GetRequiredService<EmailComposer>();
        var scopedGmailService = scope.ServiceProvider.GetRequiredService<IGmailEmailService>();

        await SendCancellationEmailAsync(scopedEmailComposer, scopedGmailService, recipient, cancelledEvent,
            reasonForCancellation);
    }

    private async Task<List<Event>> GetCollidingEventsAsync(Event newEvent, CancellationToken ct = default)
    {
        var existingEvents = await db.Events
            .Where(e => e.Start < newEvent.End && e.End > newEvent.Start)
            .ToListAsync(ct);

        return existingEvents;
    }

    private async Task SendCancellationEmailAsync(
        EmailComposer emailComposer,
        IGmailEmailService gmailEmailService,
        string recipient,
        Event cancelledEvent,
        Email reasonForCancellation)
    {
        var body = emailComposer.ComposeCancellationEmail(recipient, cancelledEvent, reasonForCancellation);
        await gmailEmailService.ReplyToEmailAsync(
            reasonForCancellation.MessageId,
            reasonForCancellation.ThreadId,
            cancelledEvent.Title,
            recipient,
            body);
    }

    private bool ShouldReplaceEvent(List<Event> existingEvents, Event newEvent)
    {
        StringBuilder eventsInfo = new();
        eventsInfo.AppendLine("Existing events:");
        foreach (var existingEvent in existingEvents)
        {
            eventsInfo.AppendLine(EventToString(existingEvent));
            eventsInfo.AppendLine("---------------------------------");
        }

        eventsInfo.AppendLine("New event:");
        eventsInfo.AppendLine(EventToString(newEvent));

        var prompt = new PromptRequest(
            [
                new Message("system",
                    """
                    You are an assistant that helps the user decide whether to replace an existing events with a new one, which overlap.
                    The decision should be based on the importance of the events and their categories, who had send them and how crucial they are.
                    """),
                new Message("user",
                    $"""
                     {eventsInfo}
                     Should I replace the existing events with the new one?
                     Write true if yes, false if no.
                     """)
            ],
            ResponseFormat: ShouldReplaceEventSchema.RootElement);

        var response = router.SendAsync(prompt).Result;
        using var doc = JsonDocument.Parse(response.Content!);

        return doc.RootElement.GetProperty("should-change-event").GetBoolean();

        string EventToString(Event e) =>
            $"""
             Title: {e.Title}
             Date: {e.Start:yyyy-MM-dd}
             Start Time: {e.Start:HH:mm}
             End Time: {e.End:HH:mm}
             Importance: {e.Importance}
             Email Sender: {e.EventCreatedFromEmail!.SendingUserEmail}
             Email Subject: {e.Title}
             Description: {e.Description ?? "No description provided."}
             """;
    }
}
