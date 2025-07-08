using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCalendarAssistant.Data.Configuration
{
	public class UserNoteConfiguration : IEntityTypeConfiguration<UserNote>
	{
		public void Configure(EntityTypeBuilder<UserNote> builder)
		{
			builder
				.HasOne(e => e.User)
				.WithMany(u => u.UserNotes)
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	
	}
}
