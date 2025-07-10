using System.Security.Claims;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using AiCalendarAssistant.Models.DTOs;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers.ApiControllers;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NoteApiController(INoteService noteService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<List<NoteDto>>> GetAllUserNotes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notes = await noteService.GetNotesByUserIdAsync(userId);

        var noteDtos = notes.Select(n => new NoteDto
        {
            Id = n.Id,
            Title = n.Title,
            Body = n.Body,
            CreatedOn = n.CreatedOn,
            IsProcessed = n.IsProcessed
        }).ToList();

        return Ok(noteDtos);
    }
    [HttpPost("add")]
    public async Task<ActionResult<int>> AddNote([FromBody] CreateNoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var note = new UserNote
        {
            Title = request.Title,
            Body = request.Body,
            UserId = userId,
            CreatedOn = DateTime.UtcNow,
            IsProcessed = false
        };

        await noteService.AddNoteAsync(note);
        return Ok(note.Id);
    }
}