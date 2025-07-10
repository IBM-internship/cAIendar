using Microsoft.AspNetCore.Mvc;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatApiController : ControllerBase
    {
        public class MessageModel
        {
            public string Text { get; set; }
        }

        [HttpPost]
        public IActionResult ReceiveMessage([FromBody] MessageModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Text))
                return BadRequest("Text is required.");

            // Do something with model.Text later
            return Ok("Message received.");
        }
    }
}
