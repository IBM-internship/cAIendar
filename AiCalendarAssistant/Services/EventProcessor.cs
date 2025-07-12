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

    public async Task ProcessEventAsync(Event newEvent, ApplicationUser user, CancellationToken ct = default)
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
            var existingEmail =
                await db.Emails.FindAsync([newEvent.EventCreatedFromEmailId.Value], cancellationToken: ct);
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
        var shouldReplace = sameCategory && ShouldReplaceEvent(collidingEvents, newEvent, user);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Most important event importance: {mostImportantEvent.Importance}");
        Console.WriteLine($"New event importance: {newEvent.Importance}");
        Console.WriteLine($"Same category: {sameCategory}");
        Console.WriteLine($"Should replace (if same category): {shouldReplace}");
        Console.ResetColor();

        if ((sameCategory && !shouldReplace) || mostImportantEvent.Importance > newEvent.Importance)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Most important event is more important than the new event, not replacing it.");
            Console.ResetColor();

            SendAsyncFunc(SendCancellationEmailWithScopeAsync(
                newEvent.EventCreatedFromEmail!.SendingUserEmail,
                newEvent,
                mostImportantEvent.EventCreatedFromEmail!,
                user));
        }
        else if ((sameCategory && shouldReplace) || mostImportantEvent.Importance < newEvent.Importance)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Most important event is less important than the new event, replacing them.");
            Console.ResetColor();

            foreach (var collidingEvent in collidingEvents)
            {
                db.Events.Remove(collidingEvent);

                SendAsyncFunc(SendCancellationEmailWithScopeAsync(
                    collidingEvent.EventCreatedFromEmail!.SendingUserEmail,
                    collidingEvent,
                    newEvent.EventCreatedFromEmail!,
                    user));
            }

            db.Events.Add(newEvent);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SendCancellationEmailWithScopeAsync(string recipient, Event cancelledEvent,
        Email reasonForCancellation, ApplicationUser user)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Beginning sending cancellation email for event: " + cancelledEvent.Title);
        Console.ResetColor();

        using var scope = serviceScopeFactory.CreateScope();
        var scopedEmailComposer = scope.ServiceProvider.GetRequiredService<EmailComposer>();
        var scopedGmailService = scope.ServiceProvider.GetRequiredService<IGmailEmailService>();

        await SendCancellationEmailAsync(scopedEmailComposer, scopedGmailService, recipient, cancelledEvent,
            reasonForCancellation, user);
    }


    private async Task<List<Event>> GetCollidingEventsAsync(Event newEvent, CancellationToken ct = default)
    {
        var existingEvents = await db.Events
            .Include(e => e.EventCreatedFromEmail)
            .Where(e => e.Start < newEvent.End && e.End > newEvent.Start)
            .ToListAsync(ct);

        return existingEvents;
    }

    private async Task SendCancellationEmailAsync(
        EmailComposer emailComposer,
        IGmailEmailService gmailEmailService,
        string recipient,
        Event cancelledEvent,
        Email reasonForCancellation,
        ApplicationUser user) // Add user ID parameter
    {
        if (reasonForCancellation == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"Warning: reasonForCancellation is null for event {cancelledEvent.Title}. Cannot send cancellation email.");
            Console.ResetColor();
            return;
        }

        var body = await emailComposer.ComposeCancellationEmailAsync(recipient, cancelledEvent, reasonForCancellation, user);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sending the cancellation email to {recipient} for event: {cancelledEvent.Title}");
        Console.ResetColor();

        // Use the new method that accepts userId
        await gmailEmailService.ReplyToEmailAsync(
            reasonForCancellation.MessageId,
            reasonForCancellation.ThreadId,
            cancelledEvent.Title,
            recipient,
            body,
            user.Id);
    }

    private bool ShouldReplaceEvent(List<Event> existingEvents, Event newEvent, ApplicationUser user)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            "Deciding whether to replace existing events with the new one based on importance and categories.");
        Console.ResetColor();

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

        var response = router.SendAsync(prompt, user).Result;
        using var doc = JsonDocument.Parse(response.Content!);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Should replace event: {doc.RootElement.GetProperty("should-change-event").GetBoolean()}");
        Console.ResetColor();

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