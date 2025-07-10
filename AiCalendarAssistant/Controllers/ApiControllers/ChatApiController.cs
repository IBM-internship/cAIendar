using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatApiController : ControllerBase
    {
        public class MessageModel
        {
            public int ChatId { get; set; }
            public string Text { get; set; } = string.Empty;
        }
        ApplicationDbContext _db;
        public ChatApiController(ApplicationDbContext db)
        {
            _db = db;
        }
        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] MessageModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Text))
                return BadRequest("Text is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found.");

            var chat = await _db.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == model.ChatId && c.UserId == userId);

            if (chat == null)
                return NotFound("Chat not found or doesn't belong to the user.");

            var newMessage = new Message
            {
                ChatId = chat.Id,
                Role = MessageRole.User,
                Text = model.Text,
                SentOn = DateTime.Now,
                Pos = chat.Messages?.Count() ?? 0
            };

            _db.Messages.Add(newMessage);
            await _db.SaveChangesAsync();
            

            return Ok($"Message saved to chat {chat.Id}.");
        }

    }
}
