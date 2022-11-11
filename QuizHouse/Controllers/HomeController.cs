using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(HomeActionFilter))]
	public class HomeController : Controller
	{
		IUserAuthentication _userAuthentication;
		IAccountRepository _accountRepository;

		public HomeController(IUserAuthentication userAuthentication, IAccountRepository accountRepository)
		{
			_userAuthentication = userAuthentication;
			_accountRepository = accountRepository;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> Logout()
		{
			await _userAuthentication.LogoutUser(HttpContext);

			return new RedirectResult(Url.Action("Index", "Login"), false);
		}

		[HttpGet]
		public async Task<IActionResult> ResendEmail()
		{
			var account = HttpContext.Items["userAccount"] as AccountDTO;

			if (account.EmailConfirmed)
				return Json(new { error = "already_confirmed" });

			await _accountRepository.SendConfirmationEmail(account, Url);

			return Json(new { success = "email_sended" });
		}
	}
}
