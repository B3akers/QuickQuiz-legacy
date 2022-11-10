using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.ActionFilters
{
    public class LoginActionFilter : IAsyncActionFilter
    {
        private IUserAuthentication _userAuthentication;

        public LoginActionFilter(IUserAuthentication userAuthentication)
        {
            _userAuthentication = userAuthentication;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = await _userAuthentication.GetAuthenticatedUserId(context.HttpContext);
            if (!string.IsNullOrEmpty(userId))
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };

            await next();
        }
    }
}
