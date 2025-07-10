namespace AiCalendarAssistant.Data.Models;

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
    public bool IsDeleted { get; set; } = false; // Flag to mark user as deleted without removing from database
}