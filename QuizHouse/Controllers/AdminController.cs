using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
    [AdminActionFilter]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}