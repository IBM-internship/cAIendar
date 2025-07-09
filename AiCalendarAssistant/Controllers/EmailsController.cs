using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiCalendarAssistant.Services;

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
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to fetch emails. Please try again.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string messageId, string threadId, string originalSubject, string fromEmail)
    {
        try
        {
            var hasValidToken = await tokenService.IsTokenValidAsync();
            
            if (!hasValidToken)
            {
                TempData["ErrorMessage"] = "Please log in again to access Gmail functionality.";
                return RedirectToAction("Logout", "Account");
            }

            var success = await gmail.ReplyToEmailAsync(messageId, threadId, originalSubject, fromEmail);
            
            if (success)
            {
                TempData["SuccessMessage"] = $"Reply sent successfully to {fromEmail}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send reply. Please try again.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Please log in again to access Gmail functionality.";
            return RedirectToAction("Logout", "Account");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while sending the reply.";
        }

        return RedirectToAction("Last");
    }
}