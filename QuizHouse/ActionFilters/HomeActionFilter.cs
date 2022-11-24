using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using QuizHouse.Interfaces;
using QuizHouse.Models;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.ActionFilters
{
	public class HomeActionFilter : IAsyncActionFilter
	{
		private IUserAuthentication _userAuthentication;

		public HomeActionFilter(IUserAuthentication userAuthentication)
		{
			_userAuthentication = userAuthentication;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var account = await _userAuthentication.GetAuthenticatedUser(context.HttpContext);
			if (account == null)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Login", action = "Index" })) { Permanent = false };
				return;
			}

			context.HttpContext.Items["userAccount"] = account;

			var action = (string)context.HttpContext.GetRouteValue("action");
			if (action == "Ban")
			{
				if (!string.IsNullOrEmpty(account.BanReason))
				{
					await next();
					return;
				}

				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };
				return;
			}

			if (!string.IsNullOrEmpty(account.BanReason))
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Ban" })) { Permanent = false };
				return;
			}

			if (action != "Logout" &&
				action != "ResendEmail")
			{
				if (!account.EmailConfirmed)
				{
					context.Result = new ViewResult() { ViewName = "~/Views/Home/EmailConfirm.cshtml" };
					return;
				}
			}

			await next();
		}
	}
}
