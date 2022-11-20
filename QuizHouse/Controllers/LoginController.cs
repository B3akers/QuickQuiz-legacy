using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using QuizHouse.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	public class RegisterAccountParametrs
	{
		[Required]
		[StringLength(25)]
		[MinLength(3)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*(?:\\ [a-zA-Z0-9]+)?$")]
		public string Username { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(64)]
		public string Email { get; set; }
	}

	public class RequestPasswordResetParametrs
	{
		[Required]
		[EmailAddress]
		[StringLength(64)]
		public string Email { get; set; }
	}

	public class LoginAccountParametrs
	{
		[Required]
		[StringLength(64)]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }
	}

	[TypeFilter(typeof(LoginActionFilter))]
	public class LoginController : Controller
	{
		private readonly IAccountRepository _accountRepository;
		private readonly IUserAuthentication _userAuthentication;
		private readonly IAccountConnector _accountConnector;

		public LoginController(IAccountRepository accountRepository, IUserAuthentication userAuthentication, IAccountConnector accountConnector)
		{
			_accountRepository = accountRepository;
			_userAuthentication = userAuthentication;
			_accountConnector = accountConnector;
		}


		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Register()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> TwitchLogin(string code)
		{
			var currentUser = await _userAuthentication.GetAuthenticatedUser(HttpContext);
			var userController = currentUser == null ? "Login" : "Home";

			try
			{
				if (string.IsNullOrEmpty(code))
					return new RedirectResult(Url.Action("Index", userController, new { error = "twitch_connect_error" }), false);

				var twitchToken = await _accountConnector.TwitchAuthorization(code, Url.Action("TwitchLogin", "Login", null, Request.Scheme));
				var scopes = twitchToken["scope"] as JArray;

				if (!scopes.Any(x => x.Value<string>() == "user:read:email"))
					return new RedirectResult(Url.Action("Index", userController, new { error = "twitch_invalid_scope" }), false);

				var accessToken = (string)twitchToken["access_token"];
				var refreshToken = (string)twitchToken["refresh_token"];

				var userInfo = await _accountConnector.TwitchGetUserInfo(accessToken);

				if (!userInfo.TryGetValue("email", out var emailValue) || string.IsNullOrEmpty((string)emailValue))
					return new RedirectResult(Url.Action("Index", userController, new { error = "twitch_invalid_email" }), false);

				var twitchUserId = (string)userInfo["id"];
				var twitchLogin = (string)userInfo["login"];
				var twitchDisplayName = (string)userInfo["display_name"];

				//We don't have account
				//
				if (currentUser == null)
				{
					//Try to create account
					//
					var email = (string)emailValue;
					var connectedAccount = await _accountRepository.GetAccountByEmail(email);
					if (connectedAccount != null)
					{
						if (connectedAccount.EmailConfirmed)
						{
							var twitchConnection = (TwitchConnectionDTO)connectedAccount.Connections.FirstOrDefault(x => x.Type == "Twitch");
							if (twitchConnection == null || twitchConnection.UserId != twitchUserId)
								return new RedirectResult(Url.Action("Index", userController, new { error = "twitch_email_exists" }), false);

							await _userAuthentication.AuthorizeForUser(HttpContext, connectedAccount.Id, true);
							return new RedirectResult(Url.Action("Index", "Home"), false);
						}

						//Delete account
						//
						await _accountRepository.DeleteAccount(connectedAccount);
					}

					var tempPassword = Randomizer.RandomPassword(18);
					var newAccount = await _accountRepository.CreateAccount(email, twitchLogin, tempPassword, true);
					await _accountRepository.SendTempPasswordEmail(newAccount, tempPassword);
					await _accountConnector.ConnectAccountToTwitch(newAccount, accessToken, refreshToken, twitchUserId, twitchLogin, twitchDisplayName);
					await _userAuthentication.AuthorizeForUser(HttpContext, newAccount.Id, true);
					return new RedirectResult(Url.Action("Index", "Home", new { success = "twitch_connect_success" }), false);
				}

				await _accountConnector.ConnectAccountToTwitch(currentUser, accessToken, refreshToken, twitchUserId, twitchLogin, twitchDisplayName);
				return new RedirectResult(Url.Action("Index", "Home", new { success = "twitch_connect_success" }), false);
			}
			catch { }

			return new RedirectResult(Url.Action("Index", userController, new { error = "twitch_connect_error" }), false);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmEmail(string key)
		{
			var result = await _accountRepository.TryConfirmEmail(key);

			if (result.Item1 == UserRequestConfrimStatus.WrongKey)
				return new RedirectResult(Url.Action("Index", "Login", new { error = "wrong_email_key" }), false);

			if (result.Item1 != UserRequestConfrimStatus.Confirmed)
				return new RedirectResult(Url.Action("Index", "Login"), false);

			var currentUser = await _userAuthentication.GetAuthenticatedUser(HttpContext);
			if (currentUser == null)
				await _userAuthentication.AuthorizeForUser(HttpContext, result.Item2, true);

			return new RedirectResult(Url.Action("Index", "Home", new { success = "email_confirmed" }), false);
		}

		[HttpGet]
		public async Task<IActionResult> ResetPassword(string key)
		{
			var result = await _accountRepository.TryConfirmPasswordReset(key);
			if (result.Item1 == UserRequestConfrimStatus.WrongKey)
				return new RedirectResult(Url.Action("Index", "Login", new { error = "wrong_email_key" }), false);

			if (result.Item1 != UserRequestConfrimStatus.Confirmed)
				return new RedirectResult(Url.Action("Index", "Login"), false);

			await _userAuthentication.AuthorizeForUser(HttpContext, result.Item2, true);

			return new RedirectResult(Url.Action("Index", "Home", new { success = "password_reseted" }), false);
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetParametrs model)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model_register" });

			var account = await _accountRepository.GetAccountByEmail(model.Email);
			if (account != null)
			{
				var currenTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				if (currenTime - account.LastEmailPasswordSend >= (60 * 30))
				{
					await _accountRepository.UpdateLastEmailPasswordSend(account, currenTime);
					await _accountRepository.SendPasswordResetRequest(account, Url);
				}
			}

			return Json(new { success = "password_reset_success" });
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> LoginAccount([FromBody] LoginAccountParametrs model)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model_register" });

			var account = await _accountRepository.GetAccountByEmail(model.Email);

			if (account == null || !_userAuthentication.CheckCredentials(account, model.Password))
				return Json(new { error = "invalid_credentials" });

			await _userAuthentication.AuthorizeForUser(HttpContext, account.Id, model.RememberMe);

			return Json(new { success = "login_success" });
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountParametrs model)
		{
			model.Email = model.Email.Trim();

			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model_register" });

			var accountExists = await _accountRepository.AccountExists(model.Email, model.Username);
			if (accountExists)
				return Json(new { error = "account_exists" });

			var account = await _accountRepository.CreateAccount(model.Email, model.Username, model.Password);
			await _accountRepository.SendConfirmationEmail(account, Url);

			return Json(new { success = "account_created" });
		}
	}
}
