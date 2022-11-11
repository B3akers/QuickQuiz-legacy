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
        public HomeController(IUserAuthentication userAuthentication) 
        {
            _userAuthentication = userAuthentication;
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
    }
}
