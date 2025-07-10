using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using PromptingPipeline.Services;            // ChatMessager
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
        public record MessageModel(int ChatId, string Text);

        private readonly ApplicationDbContext _db;
        private readonly ChatMessager         _chatMessager;

        public ChatApiController(
            ApplicationDbContext db,
            ChatMessager         chatMessager)      // injected
        {
            _db           = db;
            _chatMessager = chatMessager;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveMessage(
            [FromBody] MessageModel model,
            CancellationToken       ct)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.Text))
                return BadRequest("Text is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found.");

            var chat = await _db.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(
                    c => c.Id == model.ChatId && c.UserId == userId,
                    ct);

            if (chat is null)
                return NotFound("Chat not found or doesn't belong to the user.");

            // ── 1) save the user message ───────────────────────────────────────
            var userMessage = new Message
            {
                ChatId = chat.Id,
                Role   = MessageRole.User,
                Text   = model.Text,
                SentOn = DateTime.UtcNow,
                Pos    = chat.Messages?.Count() ?? 0
            };

            _db.Messages.Add(userMessage);
            await _db.SaveChangesAsync(ct);

            // ── 2) let the assistant respond via ChatMessager ──────────────────
			var assistantMessage =
                await _chatMessager.GenerateAssistantMessageAsync(chat, ct);

            // ── 3) return both IDs & the assistant text ────────────────────────
            return Ok(new
            {
                chatId            = chat.Id,
                userMessageId     = userMessage.Id,
                assistantMessageId = assistantMessage.Id,
                assistantText     = assistantMessage.Text
            });
        }
    }
}

