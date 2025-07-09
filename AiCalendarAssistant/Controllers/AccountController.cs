using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Controllers;

[AllowAnonymous]
public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? returnUrl)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        properties.IsPersistent = true;
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl, string? remoteError)
    {
        returnUrl = returnUrl ?? Url.Content("~/");

        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return RedirectToPage("/Account/Login", new { Area = "Identity" });
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("/Account/Login", new { Area = "Identity" });
        }

        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: true, bypassTwoFactor: true);

        if (!result.Succeeded)
            return result.IsLockedOut
                ? RedirectToPage("/Identity/Account/Lockout")
                : RedirectToPage("/Account/ExternalLogin",
                    new { Area = "Identity", ReturnUrl = returnUrl, LoginProvider = info.LoginProvider });
            
        await signInManager.UpdateExternalAuthenticationTokensAsync(info);
        return LocalRedirect(returnUrl);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}