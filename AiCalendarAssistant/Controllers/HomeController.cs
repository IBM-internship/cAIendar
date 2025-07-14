using System.Diagnostics;
using AiCalendarAssistant.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers;

public class HomeController : BaseController
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Calendar()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}