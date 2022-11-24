using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Models;
using QuizHouse.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace QuizHouse.Controllers
{
	[TypeFilter(typeof(ModeratorActionFilter))]
	public class ModeratorController : Controller
	{
		private readonly DatabaseService _databaseService;
		private readonly ICdnUploader _cdnUploader;
		public ModeratorController(DatabaseService databaseService, ICdnUploader cdnUploader)
		{
			_databaseService = databaseService;
			_cdnUploader = cdnUploader;
		}

		public IActionResult Index()
		{
			return new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Moderator", action = "Reports" })) { Permanent = true };
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeclineReport([FromBody] DeleteRecordModel paramters)
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
			await accounts.UpdateManyAsync(Builders<AccountDTO>.Filter.In(x => x.Id, report.Accounts), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, -1).Inc(x => x.ActiveReports, -1));

			return Json(new { success = "report_decline" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeclineRequest([FromBody] DeleteRecordModel paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var requests = _databaseService.GetQuestionRequestsCollection();
			var request = await (await requests.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
			if (request == null || request.Result != QuestionRequestResult.None)
				return Json(new { success = "report_handled" });

			if (!string.IsNullOrEmpty(request.Image))
			{
				var fileName = request.Image.Substring(request.Image.LastIndexOf('/') + 1);
				var filePath = Path.Combine("wwwroot", "uploads", fileName);

				System.IO.File.Delete(filePath);
			}

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var accounts = _databaseService.GetAccountsCollection();
			await requests.UpdateOneAsync(x => x.Id == request.Id, Builders<QuestionRequestDTO>.Update.Set(x => x.Result, QuestionRequestResult.Declined).Set(x => x.ModeratorId, account.Id));
			await accounts.UpdateOneAsync(Builders<AccountDTO>.Filter.Eq(x => x.Id, request.Author), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, -1).Inc(x => x.ActiveReports, -1));

			return Json(new { success = "report_decline" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AcceptRequest([FromBody] ModifyQuestionModel paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			paramters.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (paramters.SelectedCategories.Count == 0)
				return Json(new { error = "invalid_model" });

			var requests = _databaseService.GetQuestionRequestsCollection();
			var request = await (await requests.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
			if (request == null || request.Result != QuestionRequestResult.None)
				return Json(new { success = "report_handled" });

			if (!string.IsNullOrEmpty(request.Image))
				request.Image = await _cdnUploader.UploadFileAsync(request.Image);

			var questions = _databaseService.GetQuestionsCollection();
			var question = new QuestionDTO() { Text = paramters.Label, Answers = new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }, CorrectAnswer = paramters.CorrectAnswer, Image = request.Image, Categories = paramters.SelectedCategories, Author = request.Author };
			await questions.InsertOneAsync(question);

			var categoriesCollection = _databaseService.GetCategoryCollection();
			await categoriesCollection.UpdateManyAsync(Builders<CategoryDTO>.Filter.In(x => x.Id, question.Categories), Builders<CategoryDTO>.Update.Inc(x => x.QuestionCount, 1));

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var accounts = _databaseService.GetAccountsCollection();
			await requests.UpdateOneAsync(x => x.Id == request.Id, Builders<QuestionRequestDTO>.Update.Set(x => x.Result, QuestionRequestResult.Accepted).Set(x => x.ModeratorId, account.Id).Set(x => x.QuestionId, question.Id));
			await accounts.UpdateOneAsync(Builders<AccountDTO>.Filter.Eq(x => x.Id, request.Author), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, 1).Inc(x => x.ActiveReports, -1));

			return Json(new { success = "report_accepted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AcceptReport([FromBody] ModifyQuestionModel paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			paramters.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (paramters.SelectedCategories.Count == 0)
				return Json(new { error = "invalid_model" });

			var reports = _databaseService.GetQuestionReportsCollection();
			var report = await (await reports.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
			if (report == null || report.Status != ReportResultDTO.None)
				return Json(new { success = "report_handled" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			var accounts = _databaseService.GetAccountsCollection();
			await reports.UpdateOneAsync(x => x.Id == report.Id, Builders<QuestionReportDTO>.Update.Set(x => x.Status, ReportResultDTO.Approved).Set(x => x.ModeratorId, account.Id));
			await accounts.UpdateManyAsync(Builders<AccountDTO>.Filter.In(x => x.Id, report.Accounts), Builders<AccountDTO>.Update.Inc(x => x.ReportWeight, 1).Inc(x => x.ActiveReports, -1));

			var questions = _databaseService.GetQuestionsCollection();
			await questions.UpdateOneAsync(x => x.Id == report.QuestionId, Builders<QuestionDTO>.Update.Set(x => x.Text, paramters.Label).Set(x => x.CorrectAnswer, paramters.CorrectAnswer).Set(x => x.Answers, new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }).Set(x => x.Categories, paramters.SelectedCategories));

			return Json(new { success = "report_accepted" });
		}

		public async Task<IActionResult> Questions()
		{
			var requests = _databaseService.GetQuestionRequestsCollection();
			var model = new ModeratorQuestionsModel();

			model.Questions = await (await requests.FindAsync(x => x.Result == QuestionRequestResult.None, new FindOptions<QuestionRequestDTO>() { Limit = 10, Sort = Builders<QuestionRequestDTO>.Sort.Descending(x => x.Prority).Ascending(x => x.CreationTime) })).ToListAsync();

			return View(model);
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
