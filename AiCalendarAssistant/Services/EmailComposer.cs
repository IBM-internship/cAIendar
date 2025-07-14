using System.Text;
using System.Text.Json;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using Message = AiCalendarAssistant.Models.Message;

namespace AiCalendarAssistant.Services;

public class EmailComposer(PromptRouter router)
{
    private static readonly JsonDocument ReasonForCancellationSummarySchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "reason_for_cancellation_summary",
            "strict": false,
            "schema": {
              "type": "object",
              "properties": {
                "summary": { "type": "string" }
              },
              "required": ["summary"],
              "additionalProperties": false
            }
          }
        }
        """);

    private static readonly JsonDocument CancellationEmailSchema = JsonDocument.Parse(
        """
        {
          "type": "json_schema",
          "json_schema": {
            "name": "cancellation_email",
            "strict": false,
            "schema": {
              "type": "object",
              "properties": {
                "email-body": { "type": "string" }
              },
              "required": ["email-body"],
              "additionalProperties": false
            }
          }
        }
        """);

    public async Task<string> ComposeCancellationEmailAsync(string recipient, Event cancelledEvent,
        Email reasonForCancellation, ApplicationUser user)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Composing cancellation email.");
        Console.ResetColor();

        var reasonForCancellationSummary = await GetCancellationReasonSummaryAsync(reasonForCancellation, user);
        return await ComposeCancellationEmailAsync(recipient, cancelledEvent, reasonForCancellationSummary, user);
    }

    private async Task<string> GetCancellationReasonSummaryAsync(Email? reasonForCancellation, ApplicationUser user)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Composing reason for cancellation summary.");
        Console.ResetColor();

        // Add null check for reasonForCancellation
        if (reasonForCancellation == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: reasonForCancellation is null, using default summary.");
            Console.ResetColor();
            return "A higher priority event has been scheduled.";
        }

        try
        {
            var prompt = new PromptRequest(
            [
                new Message("system",
                    """
                    You are an assistant that summarises reasons for event cancellations.
                    You will be provided with an email that contains the reason for cancellation.
                    You have to write an email summarising the email, because of which some event has been cancelled
                    (the reason for the cancellation is more important than the other event).
                    Provide a not specific summary of the reason for cancellation based on the email content.
                    Do not include any personal information or specific details about the event.
                    """),
                new Message("user",
                    $"""
                     Summarise the reason for cancellation from the following email:

                     From: {reasonForCancellation.SendingUserEmail ?? "Unknown"}
                     Subject: {reasonForCancellation.Title ?? "No Subject"}
                     Body: {reasonForCancellation.Body ?? "No content"}
                     """)
            ], ResponseFormat: ReasonForCancellationSummarySchema.RootElement);

            var response = await router.SendAsync(prompt, user); // Make it properly async

            if (response?.Content == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: Received null response from router, using default summary.");
                Console.ResetColor();
                return "A higher priority event has been scheduled.";
            }

            using var doc = JsonDocument.Parse(response.Content);
            var summary = doc.RootElement.GetProperty("summary").GetString();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Reason for cancellation summary: {summary ?? "No summary provided"}");
            Console.ResetColor();

            return summary ?? "A higher priority event has been scheduled.";
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Error getting cancellation reason summary: {ex.Message}");
            Console.ResetColor();
            return "A higher priority event has been scheduled.";
        }
    }

    private async Task<string> ComposeCancellationEmailAsync(string recipient, Event cancelledEvent,
        string reasonForCancellationSummary, ApplicationUser user)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Composing cancellation email for {recipient} regarding event {cancelledEvent.Title}.");
        Console.ResetColor();

        try
        {
            // TODO: add more context for the recipient (is he a boss or a colleague, etc.)
            var prompt = new PromptRequest(
            [
                new Message("system",
                    """
                    You are an assistant that composes emails to inform users about calendar event cancellations. 
                    The email should be polite and informative, explaining the reason for the cancellation.
                    Use an appropriate tone according to the context of the cancellation and the recipient of the cancellation email.
                    """),
                new Message("user",
                    $"""
                     Compose an email to {recipient} informing them that the following event has been cancelled:

                     Title: {cancelledEvent.Title}
                     Date: {cancelledEvent.Start:yyyy-MM-dd}
                     Start Time: {cancelledEvent.Start:HH:mm}
                     End Time: {cancelledEvent.End:HH:mm}
                     Reason for cancellation: {reasonForCancellationSummary}
                     Use language according to the context of the email that is being cancelled, and the tone or relationship with the recipient.
                     Talk to the recipient in first person, as if you were the one sending the email.
                     Use the user's information to make the email more personal, if available.
                     """)
            ], ResponseFormat: CancellationEmailSchema.RootElement);

            var response = await router.SendAsync(prompt, user); // Make it properly async

            if (response?.Content == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: Received null response from router, using default email body.");
                Console.ResetColor();
                return CreateDefaultCancellationEmail(recipient, cancelledEvent, reasonForCancellationSummary);
            }

            using var doc = JsonDocument.Parse(response.Content);
            var emailBody = doc.RootElement.GetProperty("email-body").GetString();

            StringBuilder emailBodyBuilder = new();
            emailBodyBuilder.AppendLine(emailBody ??
                                        CreateDefaultCancellationEmail(recipient, cancelledEvent,
                                            reasonForCancellationSummary));
            emailBodyBuilder.AppendLine();
            emailBodyBuilder.AppendLine("[ This response was generated by an AI assistant ]");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Cancellation email body:\n{emailBodyBuilder}");
            Console.ResetColor();

            return emailBodyBuilder.ToString();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Error composing cancellation email: {ex.Message}");
            Console.ResetColor();
            return CreateDefaultCancellationEmail(recipient, cancelledEvent, reasonForCancellationSummary);
        }
    }

    private static string CreateDefaultCancellationEmail(string recipient, Event cancelledEvent,
        string reasonForCancellationSummary)
    {
        return $"""
                Dear {recipient},

                I hope this email finds you well.

                I am writing to inform you that the following event has been cancelled:

                Event: {cancelledEvent.Title}
                Date: {cancelledEvent.Start:yyyy-MM-dd}
                Time: {cancelledEvent.Start:HH:mm} - {cancelledEvent.End:HH:mm}

                Reason for cancellation: {reasonForCancellationSummary}

                I apologize for any inconvenience this may cause.

                Best regards,
                Your Calendar Assistant

                [ This response was generated by an AI assistant ]
                """;
    }
}