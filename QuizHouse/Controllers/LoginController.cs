using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
    public class RegisterAccountParametrs
    {
        [Required]
        [StringLength(25)]
        [MinLength(3)]
        [RegularExpression("^[a-zA-Z0-9 _]*$")]
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

    public class LoginController : Controller
    {
        private IAccountRepository _accountRepository;
        private IUserAuthentication _userAuthentication;

        public LoginController(IAccountRepository accountRepository, IUserAuthentication userAuthentication)
        {
            _accountRepository = accountRepository;
            _userAuthentication = userAuthentication;
        }

        [TypeFilter(typeof(LoginActionFilter))]
        public IActionResult Index()
        {
            return View();
        }

        [TypeFilter(typeof(LoginActionFilter))]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string key)
        {
            var result = await _accountRepository.TryConfirmEmail(key);

            if (result.Item1 == EmailConfirmationStatus.WrongKey)
                return new RedirectResult(Url.Action("Index", "Login", new { error = "wrong_email_key" }), false);

            if (result.Item1 != EmailConfirmationStatus.Confirmed)
                return new RedirectResult(Url.Action("Index", "Login"), false);

            var currentUser = await _userAuthentication.GetAuthenticatedUser(HttpContext);
            if (currentUser == null)
                await _userAuthentication.AuthorizeForUser(HttpContext, result.Item2, true);

            return new RedirectResult(Url.Action("Index", "Home", new { success = "email_confirmed" }), false);
        }

        [TypeFilter(typeof(LoginActionFilter))]
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

        [TypeFilter(typeof(LoginActionFilter))]
        [HttpPost]
        public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountParametrs model)
        {
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
