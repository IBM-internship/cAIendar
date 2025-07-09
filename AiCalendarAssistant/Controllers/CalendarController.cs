using Microsoft.AspNetCore.Mvc;

namespace AiCalendarAssistant.Controllers
{
	public class CalendarController : BaseController
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
