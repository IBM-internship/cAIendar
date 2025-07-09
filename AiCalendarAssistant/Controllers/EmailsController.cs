using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers;

[Authorize]
public class EmailsController : BaseController
{
    private readonly GmailEmailService _gmail;

    public EmailsController(GmailEmailService gmail)
    {
        _gmail = gmail;
    }

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

        var emails = await _gmail.GetLastEmailsAsync();
        return View(emails);
    }
}