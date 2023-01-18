using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using QuickQuiz.Interfaces;
using QuickQuiz.Services;
using System.Threading.Tasks;

namespace QuickQuiz.ActionFilters
{
	public class LobbyActionFilter : IAsyncActionFilter
	{
		private readonly IUserAuthentication _userAuthentication;
		private readonly GameManagerService _gameManagerService;
		private readonly LobbyManagerService _lobbyManagerService;

		public LobbyActionFilter(IUserAuthentication userAuthentication, GameManagerService gameManagerService, LobbyManagerService lobbyManagerService)
		{
			_userAuthentication = userAuthentication;
			_gameManagerService = gameManagerService;
			_lobbyManagerService = lobbyManagerService;
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

			var action = (string)context.HttpContext.GetRouteValue("action");
			var activeLobby = _lobbyManagerService.GetLobby(account.LastLobbyId);
			if (activeLobby != null)
			{
				if (action != "Index" || action != "Leave")
				{
					context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Lobby", action = "Index" })) { Permanent = false };
					return;
				}

				context.HttpContext.Items["userLobby"] = activeLobby;
			}

			await next();
		}
	}
}
