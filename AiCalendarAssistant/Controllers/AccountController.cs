using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Controllers;

[AllowAnonymous]
public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    : Controller
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

        if (result.Succeeded)
        {
            await signInManager.UpdateExternalAuthenticationTokensAsync(info);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut) return RedirectToPage("/Account/Login", new { Area = "Identity" });

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return RedirectToPage("/Account/Login", new { Area = "Identity" });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded) return RedirectToPage("/Account/Login", new { Area = "Identity" });

        createResult = await userManager.AddLoginAsync(user, info);
        if (!createResult.Succeeded) return RedirectToPage("/Account/Login", new { Area = "Identity" });

        var accessToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
        var refreshToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
        var expiresAt = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "expires_at")?.Value;

        if (!string.IsNullOrEmpty(accessToken))
        {
            await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "access_token", accessToken);
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "refresh_token", refreshToken);
        }

        if (!string.IsNullOrEmpty(expiresAt))
        {
            await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "expires_at", expiresAt);
        }

        await signInManager.SignInAsync(user, isPersistent: true);
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