using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using QuickQuiz.Interfaces;
using QuickQuiz.Models;
using QuickQuiz.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickQuiz.ActionFilters
{
	public class ApiActionFilter : IAsyncActionFilter
	{
		private IUserAuthentication _userAuthentication;

		public ApiActionFilter(IUserAuthentication userAuthentication)
		{
			_userAuthentication = userAuthentication;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var account = await _userAuthentication.GetAuthenticatedUser(context.HttpContext);
			if (account == null || !account.EmailConfirmed)
			{
				context.Result = new JsonResult(new { error = "not_authorized" });
				return;
			}

			if (!string.IsNullOrEmpty(account.BanReason))
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Ban" })) { Permanent = false };
				return;
			}

			context.HttpContext.Items["userAccount"] = account;

			await next();
		}
	}
}
