using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
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
        private AccountRepositoryService _accountRepositoryService;
        public LoginController(AccountRepositoryService accountRepositoryService)
        {
            _accountRepositoryService = accountRepositoryService;
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
        public async Task<IActionResult> RegisterAccount(RegisterAccountParametrs model)
        {
            if (!ModelState.IsValid)
                return Json(new { error = "invalid_model" });

            var accountExists = await _accountRepositoryService.AccountExists(model.Email, model.Username);
            if (accountExists)
                return Json(new { error = "account_exists" });

            return Json(new { error = "not_implemented" });
        }
    }
}
