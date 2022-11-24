using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Models;
using QuizHouse.Services;
using QuizHouse.WebSockets;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(GameActionFilter))]
	public class GameController : Controller
	{
		private readonly GameManagerService _gameManagerService;
		private readonly WebSocketGameHandler _webSocketGameHandler;

		public GameController(GameManagerService gameManagerService , WebSocketGameHandler webSocketGameHandler)
		{
			_gameManagerService = gameManagerService;
			_webSocketGameHandler = webSocketGameHandler;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SoloGame([FromBody] SoloGameModel parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "wrong_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var gameId = await _gameManagerService.CreateSoloGame(account, parametrs.CategoryId);

			if (string.IsNullOrEmpty(gameId))
				return Json(new { error = "category_not_enough_questions" });

			return Json(new { gameId });
		}

		public async Task Ws()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

				await _webSocketGameHandler.Connection(HttpContext.Items["userAccount"] as AccountDTO, webSocket);
			}
			else
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
			}
		}
	}
}