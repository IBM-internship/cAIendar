using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiCalendarAssistant.Services;
using Microsoft.AspNetCore.Authentication.Google;

namespace AiCalendarAssistant.Controllers;

[Authorize]
public class EmailsController(GmailEmailService gmail, TokenRefreshService tokenService) : BaseController
{
    public async Task<IActionResult> Last()
    {
        try
        {
            var hasValidToken = await tokenService.IsTokenValidAsync();
        
            if (!hasValidToken)
            {
                TempData["ErrorMessage"] = "Please log in again to access Gmail functionality.";
                return RedirectToAction("Logout", "Account");
            }
        
            var emails = await gmail.GetLastEmailsAsync();
            return View(emails);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Please log in again to access Gmail functionality.";
            return RedirectToAction("Logout", "Account");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Unable to fetch emails. Please try again.");
            return RedirectToAction("Index", "Home");
        }
    }
    
    public IActionResult LinkGoogleAccount()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Emails"),
            IsPersistent = true
        };

        properties.SetParameter("access_type", "offline");
        properties.SetParameter("prompt", "consent");

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
    
        if (result.Succeeded)
        {
            return RedirectToAction("Last");
        }
    
        ModelState.AddModelError("", "Failed to authenticate with Google");
        return RedirectToAction("Index", "Home");
    }
}