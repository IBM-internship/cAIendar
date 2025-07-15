using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services;

public class TaskService(ApplicationDbContext db) : ITaskService
{
    public async Task AddTaskAsync(UserTask task)
    {
        db.UserTasks.Add(task);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var existingTask = await db.UserTasks.FindAsync(taskId);
        if (existingTask == null)
            return false;

        db.UserTasks.Remove(existingTask);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UserTask?> GetTaskByIdAsync(int taskId)
    {
        return await db.UserTasks.FindAsync(taskId);
    }

    public async Task<bool> ReplaceTaskAsync(UserTask updatedTask)
    {
        var existingTask = await db.UserTasks.FindAsync(updatedTask.Id);
        if (existingTask == null)
            return false;

        db.Entry(existingTask).CurrentValues.SetValues(updatedTask);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserTask>> GetAllTasksAsync()
    {
        return await Task.Run(() => db.UserTasks.AsNoTracking().ToList());
    }

    public async Task<List<UserTask>> GetTasksAsync(Func<UserTask, bool> predicate)
    {
        return await Task.Run(() => db.UserTasks.AsNoTracking().Where(predicate).ToList());
    }
    public async Task<List<UserTask>> GetTasksInDateRangeAsync(DateOnly startDate, DateOnly endDate, string userId)
    {
        return await db.UserTasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
            .ToListAsync();
    }
}