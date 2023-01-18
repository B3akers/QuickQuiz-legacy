using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using QuickQuiz.ActionFilters;
using QuickQuiz.Dto;
using QuickQuiz.Interfaces;
using QuickQuiz.Models;
using QuickQuiz.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace QuickQuiz.Controllers
{
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
			return View();
		}

		public IActionResult Ban()
		{
			return View();
		}

		public async Task<IActionResult> Index()
		{
			var model = new HomeIndexModel();
			model.Categories = await _databaseService.GetCategoriesAsync();

			return View(model);
		}

		[HttpGet("Home/Profile/{profileId}")]
		public async Task<IActionResult> Profile(string profileId)
		{
			var model = new HomeProfileModel();

			var visitorAccount = await _accountRepository.GetAccount(profileId);
			if (visitorAccount == null)
			{
				model.AccountNotFound = true;
				return View(model);
			}

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			if (visitorAccount.ProfilPrivate && !account.IsAdmin && !account.IsModerator)
			{
				model.AccountPrivate = true;
				return View(model);
			}

			model.Account = visitorAccount;

			if (visitorAccount.Id == account.Id || account.IsAdmin || account.IsModerator)
				model.QuestionRequests = await (await _databaseService.GetQuestionRequestsCollection().FindAsync(x => x.Author == visitorAccount.Id, new FindOptions<QuestionRequestDTO>() { Limit = 10, Sort = Builders<QuestionRequestDTO>.Sort.Descending(x => x.Id) })).ToListAsync();

			return View(model);
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> SetUserPreferences([FromBody] SetUserPreferencesModel parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var accounts = _databaseService.GetAccountsCollection();
			var account = HttpContext.Items["userAccount"] as AccountDTO;

			await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.StreamerMode, parametrs.StreamerMode).Set(x => x.ProfilPrivate, parametrs.PrivateProfil).Set(x => x.UserColor, parametrs.Color));

			return Json(new { success = "preferences_setted" });
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> RemoveAccountConnection([FromBody] RemoveAccountConnectionModel parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			await _accountConnector.RemoveConnection(account, parametrs.ConnectionType);

			return Json(new { success = "connection_removed" });
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameModel parametrs)
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

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel parametrs)
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

		[ValidateAntiForgeryToken]
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
