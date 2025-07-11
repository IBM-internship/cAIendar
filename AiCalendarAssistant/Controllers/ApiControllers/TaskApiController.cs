using System.Security.Claims;
using AiCalendarAssistant.Models.DTOs;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers.ApiControllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TaskApiController(ITaskService taskService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<List<UserTaskDto>>> GetAllTasks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tasks = await taskService.GetTasksAsync(t => t.UserId == userId);
        var dtoList = tasks.Select(UserTaskDto.FromTask).ToList();
        return Ok(dtoList);
    }

    [HttpPost("add")]
    public async Task<ActionResult<int>> AddTask([FromBody] UserTaskDto newTaskDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var newTask = newTaskDto.ToTask(userId);
        await taskService.AddTaskAsync(newTask);
        return Ok(newTask.Id);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> DeleteTask([FromBody] DeleteTaskRequest request)
    {
        var existing = await taskService.GetTaskByIdAsync(request.Id);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (existing == null || existing.UserId != userId)
            return NotFound($"Task with ID {request.Id} not found or unauthorized.");

        var success = await taskService.DeleteTaskAsync(request.Id);
        if (!success)
            return NotFound($"Task with ID {request.Id} could not be deleted.");
        return NoContent();
    }

    [HttpPut("replace")]
    public async Task<ActionResult> ReplaceTask([FromBody] UserTaskDto updatedDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existing = await taskService.GetTaskByIdAsync(updatedDto.Id);
        if (existing == null || existing.UserId != userId)
            return NotFound($"Task with ID {updatedDto.Id} not found or unauthorized.");

        var updatedTask = updatedDto.ToTask(userId!);
        await taskService.ReplaceTaskAsync(updatedTask);
        return NoContent();
    }
    [HttpPost("range")]
    public async Task<ActionResult<List<UserTaskDto>>> GetTasksInRange([FromBody] TaskDateRangeRequest range)
    {
        if (range.End < range.Start)
            return BadRequest("End date must be on or after start date.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tasks = await taskService.GetTasksInDateRangeAsync(range.Start, range.End, userId);
        var dtoList = tasks.Select(UserTaskDto.FromTask).ToList();
        return Ok(dtoList);
    }

    public class TaskDateRangeRequest
    {
        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }
    }

    public class DeleteTaskRequest
    {
        public int Id { get; set; }
    }
}