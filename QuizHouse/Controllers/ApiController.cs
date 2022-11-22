using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
	public class ReportQuestionParametrs
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[Range(0, (int)ReportReasonDTO.Other)]
		public ReportReasonDTO ReportReason { get; set; }
	};

	[TypeFilter(typeof(ApiActionFilter))]
	public class ApiController : Controller
	{
		private readonly DatabaseService _databaseService;
		private readonly GameManagerService _gameManagerService;
		public ApiController(DatabaseService databaseService, GameManagerService gameManagerService)
		{
			_databaseService = databaseService;
			_gameManagerService = gameManagerService;
		}

		[HttpGet]
		[ResponseCache(Duration = 3600)]
		public async Task<IActionResult> GetCategories()
		{
			return Json(await _databaseService.GetCategoriesAsync());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReportQuestion([FromBody] ReportQuestionParametrs parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			if (account.ReportWeight < -10) //TODO make some settings for that?
				return Json(new { error = "invalid_model" });

			var game = _gameManagerService.GetActiveGame(account.LastGameId);
			if (game == null)
				return Json(new { error = "game_not_found" });

			if (!game.Questions.Contains(parametrs.Id))
				return Json(new { error = "game_not_found" });

			var questionReports = _databaseService.GetQuestionReportsCollection();
			var report = await (await questionReports.FindAsync(x => x.QuestionId == parametrs.Id && x.Status == ReportResultDTO.None)).FirstOrDefaultAsync();
			if (report == null)
			{
				report = new QuestionReportDTO() { CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Status = ReportResultDTO.None, Reason = parametrs.ReportReason, QuestionId = parametrs.Id, Prority = Math.Max(0, account.ReportWeight), Accounts = new List<string>() { account.Id } };
				await questionReports.InsertOneAsync(report);
			}
			else if (!report.Accounts.Contains(account.Id))
				await questionReports.UpdateOneAsync(x => x.Id == report.Id, Builders<QuestionReportDTO>.Update.Inc(x => x.Prority, Math.Max(0, account.ReportWeight)).AddToSet(x => x.Accounts, account.Id));

			return Json(new { success = "report_submitted" });
		}
	}
}
