using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(LobbyActionFilter))]
	public class LobbyController : Controller
	{
		public IActionResult CustomGame()
		{
			return View();
		}
	}
}
