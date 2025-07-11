using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AiCalendarAssistant.Services;
using AiCalendarAssistant.Services.Contracts;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Controllers;

[Authorize]
public class EmailsController(
    IGmailEmailService gmail, 
    TokenRefreshService tokenService,
    UserManager<ApplicationUser> userManager) : BaseController
{
    public async Task<IActionResult> Last()
    {
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Logout", "Account");
            }

            var hasValidToken = await tokenService.IsTokenValidAsync(user.Id);
        
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
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Logout", "Account");
            }

            var hasValidToken = await tokenService.IsTokenValidAsync(user.Id);
            
            if (!hasValidToken)
            {
                TempData["ErrorMessage"] = "Please log in again to access Gmail functionality.";
                return RedirectToAction("Logout", "Account");
            }

            var success = await gmail.ReplyToEmailAsync(messageId, threadId, originalSubject, fromEmail, "this is a test reply", user.Id);
            
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
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred while sending the reply.";
        }

        return RedirectToAction("Last");
    }
}