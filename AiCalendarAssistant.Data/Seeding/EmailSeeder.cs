using AiCalendarAssistant.Data.Constants;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Data.Seeding
{
    public class EmailSeeder
    {
        public static IEnumerable<Email> SeedEmails()
        {
            List<Email> emails = new List<Email>
            {
                new Email
                {
                    Id = EmailConstants.Email1Id,
                    GmailMessageId = EmailConstants.Email1GmailMessageId,
                    Title = EmailConstants.Email1Title,
                    CreatedOn = new DateTime(2025, 1, 1, 9, 0, 0), // January 1, 2025, 9:00 AM
                    Body = EmailConstants.Email1Body,
                    SendingUserEmail = EmailConstants.Email1SendingUserEmail,
                    RecievingUserId = EmailConstants.Email1RecievingUserId,
                    IsProcessed = EmailConstants.Email1IsProcessed,
                    ThreadId = EmailConstants.Email1ThreadId,
                    MessageId = EmailConstants.Email1MessageId,
                    IsDeleted = EmailConstants.Email1IsDeleted
                }
            };

            return emails;
        }
    }
}
