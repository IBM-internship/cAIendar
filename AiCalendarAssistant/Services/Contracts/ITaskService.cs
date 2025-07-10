using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services.Contracts;

public interface ITaskService
{
    Task AddTaskAsync(UserTask task);
    Task<bool> DeleteTaskAsync(int taskId);
    Task<UserTask?> GetTaskByIdAsync(int taskId);
    Task<bool> ReplaceTaskAsync(UserTask updatedTask);
    Task<List<UserTask>> GetAllTasksAsync();
    Task<List<UserTask>> GetTasksAsync(Func<UserTask, bool> predicate);
    Task<List<UserTask>> GetTasksInDateRangeAsync(DateOnly startDate, DateOnly endDate, string userId);
}