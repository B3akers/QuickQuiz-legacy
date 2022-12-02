using Microsoft.AspNetCore.Mvc;
using QuickQuiz.ActionFilters;

namespace QuickQuiz.Controllers
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
