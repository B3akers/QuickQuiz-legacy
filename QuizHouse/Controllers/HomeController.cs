using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Models;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	public class ChangePasswordParametrs
	{
		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string CurrentPassword { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }
	}

	public class RemoveAccountConnectionParametrs
	{
		[Required]
		[StringLength(64)]
		[MinLength(3)]
		public string ConnectionType { get; set; }
	}

	public class ChangeUsernameParametrs
	{
		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string CurrentPassword { get; set; }

		[Required]
		[StringLength(25)]
		[MinLength(3)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*(?:\\ [a-zA-Z0-9]+)?$")]
		public string Username { get; set; }
	}

	[TypeFilter(typeof(HomeActionFilter))]
	public class HomeController : Controller
	{
		private readonly IUserAuthentication _userAuthentication;
		private readonly IAccountRepository _accountRepository;
		private readonly IAccountConnector _accountConnector;
		private readonly DatabaseService _databaseService;

		public HomeController(IUserAuthentication userAuthentication, IAccountRepository accountRepository, DatabaseService databaseService, IAccountConnector accountConnector)
		{
			_userAuthentication = userAuthentication;
			_accountRepository = accountRepository;
			_databaseService = databaseService;
			_accountConnector = accountConnector;
		}

		public IActionResult UserSettings()
		{
			var account = HttpContext.Items["userAccount"] as AccountDTO;

			var model = new UserSettingsModel();
			model.AccountConnections = account.Connections;
			model.Username = account.Username;
			model.ModelNavBar = new NavBarModel() { Username = account.Username };

			return View(model);
		}

		public async Task<IActionResult> Index()
		{
			var account = HttpContext.Items["userAccount"] as AccountDTO;

			var model = new HomeIndexModel();
			model.ModelNavBar = new NavBarModel() { Username = account.Username };
			model.Categories = await _databaseService.GetCategoriesAsync();

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> RemoveAccountConnection([FromBody] RemoveAccountConnectionParametrs parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			await _accountConnector.RemoveConnection(account, parametrs.ConnectionType);

			return Json(new { success = "connection_removed" });
		}

		[HttpPost]
		public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameParametrs parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			if (await _accountRepository.AccountExists(null, parametrs.Username))
				return Json(new { error = "account_exists" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;

			if (!_userAuthentication.CheckCredentials(account, parametrs.CurrentPassword))
				return Json(new { error = "invalid_password" });

			await _accountRepository.ChangeUsername(account, parametrs.Username);

			return Json(new { success = "username_changed" });
		}

		[HttpPost]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordParametrs parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;

			if (!_userAuthentication.CheckCredentials(account, parametrs.CurrentPassword))
				return Json(new { error = "invalid_password" });

			await _accountRepository.ChangePassword(account, parametrs.Password, true);
			await _userAuthentication.AuthorizeForUser(HttpContext, account.Id, string.IsNullOrEmpty(HttpContext.Request.Cookies["deviceKey"]) == false);

			return Json(new { success = "password_changed" });
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
