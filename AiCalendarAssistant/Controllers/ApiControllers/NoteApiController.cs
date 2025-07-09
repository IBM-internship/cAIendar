using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiCalendarAssistant.Controllers
{
    [ApiController]
    [Route("api/notes")]
    [Authorize]
    public class NoteApiController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NoteApiController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<NoteDto>>> GetAllUserNotes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notes = await _noteService.GetNotesByUserIdAsync(userId);

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

            await _noteService.AddNoteAsync(note);
            return Ok(note.Id);
        }
    }
}
