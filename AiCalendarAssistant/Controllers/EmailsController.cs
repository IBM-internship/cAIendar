using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiCalendarAssistant.Services;
using Microsoft.AspNetCore.Authentication.Google;

namespace AiCalendarAssistant.Controllers;

[Authorize]
public class EmailsController(GmailEmailService gmail) : BaseController
{
    public async Task<IActionResult> Last()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return RedirectToPage(
                "/Account/Register",
                new { area = "Identity", returnUrl = Url.Action("Last", "Emails") }
            );
        }

        var token = await HttpContext.GetTokenAsync(GoogleDefaults.AuthenticationScheme, "access_token");
    
        if (string.IsNullOrEmpty(token))
        {
            return Challenge(
                new AuthenticationProperties 
                { 
                    RedirectUri = Url.Action("Last", "Emails"),
                    IsPersistent = true
                },
                GoogleDefaults.AuthenticationScheme
            );
        }

        var emails = await gmail.GetLastEmailsAsync();
        return View(emails);
    }
}