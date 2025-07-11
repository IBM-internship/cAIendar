using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiCalendarAssistant.Data.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder
            .HasOne(e => e.User)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.EventCreatedFromEmail)
            .WithOne(e => e.EmailCreatedEvent)
            .HasForeignKey<Event>(e => e.EventCreatedFromEmailId) // FK is in Event
            .OnDelete(DeleteBehavior.Restrict);
    }
}