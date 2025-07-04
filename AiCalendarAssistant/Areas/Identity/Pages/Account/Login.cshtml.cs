using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AiCalendarAssistant.Areas.Identity.Pages.Account
{
    public class LoginModel(SignInManager<IdentityUser> signInManager) : PageModel
    {
        public string ReturnUrl { get; set; } = string.Empty;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect(returnUrl ?? "/Calendar");
                return;
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }
    }
}