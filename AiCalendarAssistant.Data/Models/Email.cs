using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCalendarAssistant.Data.Models
{
	public class Email
	{
		//title, datecreated, timecreated, body, sendingUser, recievingUser
		public int Id { get; set; }
		public string Title { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Body { get; set; }
		public string? SendingUserEmail { get; set; }

		
		public string RecievingUserId { get; set; }
		public ApplicationUser RecievingUser { get; set; }
		public bool IsProcessed { get; set; }
	}
}
