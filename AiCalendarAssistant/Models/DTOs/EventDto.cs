using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Models.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public string? Color { get; set; }
    public string? Location { get; set; }
    public bool IsInPerson { get; set; }
    public string? MeetingLink { get; set; }

    public static EventDto FromEvent(Event e) => new EventDto
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        Start = e.Start,
        End = e.End,
        IsAllDay = e.IsAllDay,
        Color = e.Color,
        Location = e.Location,
        IsInPerson = e.IsInPerson,
        MeetingLink = e.MeetingLink
    };

    public Event ToEvent(string userId) => new Event
    {
        Id = this.Id,
        Title = this.Title,
        Description = this.Description,
        Start = this.Start,
        End = this.End,
        IsAllDay = this.IsAllDay,
        Color = this.Color,
        Location = this.Location,
        IsInPerson = this.IsInPerson,
        MeetingLink = this.MeetingLink,
        UserId = userId
    };
}