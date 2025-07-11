using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class TaskService(ApplicationDbContext context) : ITaskService
{
    public async Task AddTaskAsync(UserTask task)
    {
        context.UserTasks.Add(task);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var existingTask = await context.UserTasks.FindAsync(taskId);
        if (existingTask == null)
            return false;

        context.UserTasks.Remove(existingTask);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<UserTask?> GetTaskByIdAsync(int taskId)
    {
        return await context.UserTasks.FindAsync(taskId);
    }

    public async Task<bool> ReplaceTaskAsync(UserTask updatedTask)
    {
        var existingTask = await context.UserTasks.FindAsync(updatedTask.Id);
        if (existingTask == null)
            return false;

        context.Entry(existingTask).CurrentValues.SetValues(updatedTask);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserTask>> GetAllTasksAsync()
    {
        return await Task.Run(() => context.UserTasks.AsNoTracking().ToList());
    }

    public async Task<List<UserTask>> GetTasksAsync(Func<UserTask, bool> predicate)
    {
        return await Task.Run(() => context.UserTasks.AsNoTracking().Where(predicate).ToList());
    }
    public async Task<List<UserTask>> GetTasksInDateRangeAsync(DateOnly startDate, DateOnly endDate, string userId)
    {
        return await context.UserTasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
            .ToListAsync();
    }
}