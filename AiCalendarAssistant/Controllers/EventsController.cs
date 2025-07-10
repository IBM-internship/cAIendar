using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers;

public class EventsController : BaseController
{
	public IActionResult Index()
	{
		return View();
	}
}