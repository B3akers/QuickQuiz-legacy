using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using System.Threading.Tasks;

namespace QuizHouse.ActionFilters
{
    public class GameActionFilter : IAsyncActionFilter
    {
        private readonly IUserAuthentication _userAuthentication;
        private readonly GameManagerService _gameManagerService;

        public GameActionFilter(IUserAuthentication userAuthentication, GameManagerService gameManagerService)
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

            if (!account.EmailConfirmed)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };
                return;
            }

            context.HttpContext.Items["userAccount"] = account;

            var action = (string)context.HttpContext.GetRouteValue("action");
            var activeGame = _gameManagerService.GetActiveGame(account.LastGameId);
            if (activeGame != null)
            {
                context.HttpContext.Items["userGame"] = activeGame;

                if (action != "Index"
                    && action != "Ws")
                    context.Result = new JsonResult(new { error = "game_is_running" });
                else
                    await next();

                return;
            }

            if (action != "SoloGame")
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };
                return;
            }

            await next();
        }
    }
}
