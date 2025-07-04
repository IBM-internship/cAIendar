using System.Diagnostics;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] String message)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized("You must be logged in.");


            await Task.Delay(1000); // simulate async work

            return Ok("hi "+User.Identity.Name+" rand number: "+new Random().Next());
        }
    }
}
