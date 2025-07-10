using System.ComponentModel.DataAnnotations;

namespace AiCalendarAssistant.Data.Models
{
	public class Email
	{
		public Email()
		{
			IsProcessed = false;
        }
        //title, datecreated, timecreated, body, sendingUser, recievingUser
        [Key]
		public int Id { get; set; }
        public string GmailMessageId { get; set; } = null!;

		[Required]
        public string Title { get; set; }
		public DateTime CreatedOn { get; set; }

		[Required]
		public string Body { get; set; }
		public string? SendingUserEmail { get; set; }
        public string RecievingUserId { get; set; }
		public ApplicationUser RecievingUser { get; set; }
		public bool IsProcessed { get; set; }
		public Event? EmailCreatedEvent { get; set; } // The event created from this email
                                                      // Optional: If you want to track the event that was created when this email was processed
        public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
    }
}
