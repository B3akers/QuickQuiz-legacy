using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
    public class SoloGameParametrs
    {
        [Required]
		[MinLength(3)]
		[MaxLength(100)]
        public string CategoryId { get; set; }
	}

    [TypeFilter(typeof(GameActionFilter))]
    public class GameController : Controller
    {
        private readonly GameManagerService _gameManagerService;

        public GameController(GameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SoloGame([FromBody] SoloGameParametrs parametrs)
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

                //await _webSocketHandler.Connection(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}