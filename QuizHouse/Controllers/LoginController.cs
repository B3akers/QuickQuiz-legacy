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
        public string Email { get; set; }
    }

    [TypeFilter(typeof(LoginActionFilter))]
    public class LoginController : Controller
    {
        private IAccountRepository _accountRepository;
        public LoginController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAccount([FromBody] RegisterAccountParametrs model)
        {
            if (!ModelState.IsValid)                
                return Json(new { error = "invalid_model" });

            var accountExists = await _accountRepository.AccountExists(model.Email, model.Username);
            if (accountExists)
                return Json(new { error = "account_exists" });

            await _accountRepository.CreateAccount(model.Email, model.Username, model.Password);

            return Json(new { success = "account_created" });
        }
    }
}
