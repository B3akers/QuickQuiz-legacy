using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using QuizHouse.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.ActionFilters
{

	public class AdminActionFilter : IAsyncActionFilter
	{
		private IUserAuthentication _userAuthentication;
		private IAccountRepository _accountRepository;

		public AdminActionFilter(IUserAuthentication userAuthentication, IAccountRepository accountRepository)
		{
			_userAuthentication = userAuthentication;
			_accountRepository = accountRepository;
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

			if (!account.IsAdmin)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" })) { Permanent = false };
				return;
			}

			context.HttpContext.Items["userAccount"] = account;

			await next();
		}
	}
}
