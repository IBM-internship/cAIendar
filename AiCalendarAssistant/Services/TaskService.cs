using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddTaskAsync(UserTask task)
        {
            _context.UserTasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var existingTask = await _context.UserTasks.FindAsync(taskId);
            if (existingTask == null)
                return false;

            _context.UserTasks.Remove(existingTask);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserTask?> GetTaskByIdAsync(int taskId)
        {
            return await _context.UserTasks.FindAsync(taskId);
        }

        public async Task<bool> ReplaceTaskAsync(UserTask updatedTask)
        {
            var existingTask = await _context.UserTasks.FindAsync(updatedTask.Id);
            if (existingTask == null)
                return false;

            _context.Entry(existingTask).CurrentValues.SetValues(updatedTask);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserTask>> GetAllTasksAsync()
        {
            return await Task.Run(() => _context.UserTasks.AsNoTracking().ToList());
        }

        public async Task<List<UserTask>> GetTasksAsync(Func<UserTask, bool> predicate)
        {
            return await Task.Run(() => _context.UserTasks.AsNoTracking().Where(predicate).ToList());
        }
        public async Task<List<UserTask>> GetTasksInDateRangeAsync(DateOnly startDate, DateOnly endDate, string userId)
        {
            return await _context.UserTasks
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();
        }
    }
}
