using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Models;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(HomeActionFilter))]
	public class HomeController : Controller
	{
		private readonly IUserAuthentication _userAuthentication;
		private readonly IAccountRepository _accountRepository;
		private readonly DatabaseService _databaseService;

		public HomeController(IUserAuthentication userAuthentication, IAccountRepository accountRepository, DatabaseService databaseService)
		{
			_userAuthentication = userAuthentication;
			_accountRepository = accountRepository;
			_databaseService = databaseService;
		}

		public async Task<IActionResult> Index()
		{
			var account = HttpContext.Items["userAccount"] as AccountDTO;

			var model = new HomeIndexModel();
			model.ModelNavBar = new NavBarModel() { Username = account.Username };
			model.Categories = await _databaseService.GetCategoriesAsync();

			return View(model);
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

			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			if (currentTime - account.LastEmailConfirmSend < 900)
				return Json(new { error = "email_too_fast" });

			await _accountRepository.UpdateLastEmailConfirmSend(account, currentTime);
			await _accountRepository.SendConfirmationEmail(account, Url);

			return Json(new { success = "email_sended" });
		}
	}
}
