using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Models;
using QuizHouse.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(ModeratorActionFilter))]
	public class ModeratorController : Controller
	{
		private readonly DatabaseService _databaseService;
		public ModeratorController(DatabaseService databaseService)
		{
			_databaseService = databaseService;
		}

		public IActionResult Index()
		{
			return new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Moderator", action = "Reports" })) { Permanent = true };
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeclineReport([FromBody] DeleteRecordParamters paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var reports = _databaseService.GetQuestionReportsCollection();
			var report = await (await reports.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
			if (report == null || report.Status != ReportResultDTO.None)
				return Json(new { success = "report_handled" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var accounts = _databaseService.GetAccountsCollection();
			await reports.UpdateOneAsync(x => x.Id == report.Id, Builders<QuestionReportDTO>.Update.Set(x => x.Status, ReportResultDTO.Declined).Set(x => x.ModeratorId, account.Id));
			await accounts.UpdateManyAsync(Builders<AccountDTO>.Filter.In(x => x.Id, report.Accounts), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, -1));

			return Json(new { success = "report_decline" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AcceptReport([FromBody] ModifyQuestionParamters paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			paramters.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (paramters.SelectedCategories.Count == 0)
				return Json(new { error = "invalid_model" });

			if (string.IsNullOrEmpty(paramters.Image))
				paramters.Image = null;

			var reports = _databaseService.GetQuestionReportsCollection();
			var report = await (await reports.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
			if (report == null || report.Status != ReportResultDTO.None)
				return Json(new { success = "report_handled" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var accounts = _databaseService.GetAccountsCollection();
			await reports.UpdateOneAsync(x => x.Id == report.Id, Builders<QuestionReportDTO>.Update.Set(x => x.Status, ReportResultDTO.Approved).Set(x => x.ModeratorId, account.Id));
			await accounts.UpdateManyAsync(Builders<AccountDTO>.Filter.In(x => x.Id, report.Accounts), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, 1));

			var questions = _databaseService.GetQuestionsCollection();
			await questions.UpdateOneAsync(x => x.Id == report.QuestionId, Builders<QuestionDTO>.Update.Set(x => x.Text, paramters.Label).Set(x => x.Image, paramters.Image).Set(x => x.CorrectAnswer, paramters.CorrectAnswer).Set(x => x.Answers, new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }).Set(x => x.Categories, paramters.SelectedCategories));

			return Json(new { success = "report_accepted" });
		}

		public async Task<IActionResult> Reports()
		{
			var model = new ModeratorReportsModel();
			var reports = _databaseService.GetQuestionReportsCollection();
			var questions = _databaseService.GetQuestionsCollection();

			model.Reports = await (await reports.FindAsync(x => x.Status == ReportResultDTO.None, new FindOptions<QuestionReportDTO>() { Limit = 10, Sort = Builders<QuestionReportDTO>.Sort.Descending(x => x.Prority).Ascending(x => x.CreationTime) })).ToListAsync();
			model.Questions = await (await questions.FindAsync(Builders<QuestionDTO>.Filter.In(x => x.Id, model.Reports.Select(x => x.QuestionId)))).ToListAsync();

			return View(model);
		}
	}
}
