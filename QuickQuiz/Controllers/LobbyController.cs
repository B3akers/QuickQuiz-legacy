using Microsoft.AspNetCore.Mvc;
using QuickQuiz.ActionFilters;
using QuickQuiz.Models;
using QuickQuiz.Services;
using System.Threading.Tasks;

namespace QuickQuiz.Controllers
{
	[TypeFilter(typeof(LobbyActionFilter))]
	public class LobbyController : Controller
	{
		private readonly LobbyManagerService _lobbyManagerService;
		public LobbyController(LobbyManagerService lobbyManagerService)
		{
			_lobbyManagerService = lobbyManagerService;
		}
		public IActionResult CustomGame()
		{
			return View();
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateLobby([FromBody] LobbyCreateModel lobbyCreateModel)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			return Json(new { success = "lobby_created" });
		}
	}
}
