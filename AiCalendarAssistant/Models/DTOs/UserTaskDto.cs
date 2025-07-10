using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Models.DTOs
{
    public class UserTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public Importance Importance { get; set; } = Importance.Medium;
        public bool IsCompleted { get; set; } = false;

        public static UserTaskDto FromTask(UserTask task)
        {
            return new UserTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Color = task.Color,
                Importance = task.Importance,
                IsCompleted = task.IsCompleted
            };
        }

        public UserTask ToTask(string userId)
        {
            return new UserTask
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                Color = this.Color,
                Importance = this.Importance,
                IsCompleted = this.IsCompleted,
                UserId = userId
            };
        }
    }
}
