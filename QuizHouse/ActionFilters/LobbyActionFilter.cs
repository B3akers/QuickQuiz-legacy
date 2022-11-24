using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using System.Threading.Tasks;

namespace QuizHouse.ActionFilters
{
	public class LobbyActionFilter : IAsyncActionFilter
	{
		private readonly IUserAuthentication _userAuthentication;
		private readonly GameManagerService _gameManagerService;

		public LobbyActionFilter(IUserAuthentication userAuthentication, GameManagerService gameManagerService)
		{
			_userAuthentication = userAuthentication;
			_gameManagerService = gameManagerService;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var account = await _userAuthentication.GetAuthenticatedUser(context.HttpContext);
			if (account == null)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Login", action = "Index" })) { Permanent = false };
				return;
			}

			if (!string.IsNullOrEmpty(account.BanReason))
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Ban" })) { Permanent = false };
				return;
			}

			if (!account.EmailConfirmed)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };
				return;
			}

			context.HttpContext.Items["userAccount"] = account;

			var activeGame = _gameManagerService.GetActiveGame(account.LastGameId);
			if (activeGame != null)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Game", action = "Index" })) { Permanent = false };
				return;
			}

			await next();
		}
	}
}
