using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Models;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(AdminActionFilter))]
	public class AdminController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Categories()
		{
			return View();
		}
	}
}