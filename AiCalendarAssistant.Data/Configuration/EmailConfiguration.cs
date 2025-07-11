using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCalendarAssistant.Data.Configuration
{
	public class EmailConfiguration : IEntityTypeConfiguration<Email>
	{
		public void Configure(EntityTypeBuilder<Email> builder)
		{
			builder
				.HasOne(e => e.RecievingUser)
				.WithMany(u => u.Emails)
				.HasForeignKey(e => e.RecievingUserId)
				.OnDelete(DeleteBehavior.Cascade);

        }
	}
}
