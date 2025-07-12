using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Data.Constants
{
    public class EmailConstants
    {
        public const int Email1Id = -1;
        public const string Email1GmailMessageId = "1234567890abcdef";
        public const string Email1Title = "Test Email 1";
        public DateTime Email1CreatedOn = new DateTime(2025, 1, 1, 9, 0, 0); // January 1, 2025, 9:00 AM
        public const string Email1Body = "This is the body of the first test email.";
        public const string Email1SendingUserEmail = "sendinguser@email.com";
        public const string Email1RecievingUserId = "88bd4ce9-aece-4378-b9d5-1e5cff74b80c"; // User ID from UserConstants
        public const bool Email1IsProcessed = false;
        public const string Email1ThreadId = "thread1234567890";
        public const string Email1MessageId = "message1234567890";
        public const bool Email1IsDeleted = false; // Flag to mark email as deleted without removing from database
    }
}
