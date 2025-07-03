using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	public class EventsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
