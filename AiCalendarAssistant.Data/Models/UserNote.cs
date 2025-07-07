using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCalendarAssistant.Data.Models
{
	public class UserNote
	{
		//time created, date created, title, body, userId
		public int Id { get; set; }
		public string Title { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Body { get; set; }

		
		public string UserId { get; set; }
		public ApplicationUser User { get; set; }
		
		public bool IsProcessed { get; set; }
	}
}
