using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCalendarAssistant.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder
                .HasMany(e => e.Events)
                .WithOne(u => u.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            IEnumerable<ApplicationUser> users = UserSeeder.SeedUsers();
            builder.HasData(UserSeeder.SeedUsers());
        }
    }
}
