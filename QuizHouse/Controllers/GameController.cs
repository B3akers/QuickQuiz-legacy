using Microsoft.AspNetCore.Mvc;
using QuizHouse.ActionFilters;

namespace QuizHouse.Controllers
{
    [TypeFilter(typeof(HomeActionFilter))]
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}