using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;
using QuizHouse.Services;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(ApiActionFilter))]
	public class ApiController : Controller
	{
		private readonly DatabaseService _databaseService;
		public ApiController(DatabaseService databaseService)
		{
			_databaseService = databaseService;
		}

		[HttpGet]
		[ResponseCache(Duration = 3600)]
		public async Task<IActionResult> GetCategories()
		{
			return Json(await _databaseService.GetCategoriesAsync());
		}
	}
}
