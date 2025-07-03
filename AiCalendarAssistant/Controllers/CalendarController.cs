using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	public class CalendarController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
