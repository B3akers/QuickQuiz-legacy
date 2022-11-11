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

			if (context.HttpContext.Request.Path != "/Home/Logout" &&
				context.HttpContext.Request.Path != "/Home/ResendEmail" &&
				context.HttpContext.Request.Path != "/Home/ChangeEmail")
			{
				if (!account.EmailConfirmed)
				{
					var viewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()) { Model = new EmailConfirmModel() { Email = account.Email } };

					context.Result = new ViewResult() { ViewName = "~/Views/Home/EmailConfirm.cshtml", ViewData = viewData };
					return;
				}
			}

			context.HttpContext.Items["userAccount"] = account;

			await next();
		}
	}
}
