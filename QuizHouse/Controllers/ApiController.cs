using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Models;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
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
		public async Task<IActionResult> ReportQuestion([FromBody] ReportQuestionModel parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var account = HttpContext.Items["userAccount"] as AccountDTO;
			if (account.ReportWeight < -10 || account.ActiveReports >= 20) //TODO make some settings for that?
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
			else
				return Json(new { success = "report_submitted" });

			var accounts = _databaseService.GetAccountsCollection();
			await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Inc(x => x.ActiveReports, 1));

			return Json(new { success = "report_submitted" });
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<IActionResult> AddQuestionRequest()
		{
			if (HttpContext.Request.ContentLength > 1_048_576)
				return StatusCode((int)HttpStatusCode.RequestEntityTooLarge);

			var account = HttpContext.Items["userAccount"] as AccountDTO;

			if (account.ReportWeight < -15 || account.ActiveReports >= 20) //TODO settings?
				return Json(new { error = "invalid_model" });

			if (!HttpContext.Request.Form.TryGetValue("text", out var text)
				|| !HttpContext.Request.Form.TryGetValue("correctAnswer", out var correctAnswerStr)
				|| !HttpContext.Request.Form.TryGetValue("answer0", out var answer0)
				|| !HttpContext.Request.Form.TryGetValue("answer1", out var answer1)
				|| !HttpContext.Request.Form.TryGetValue("answer2", out var answer2)
				|| !HttpContext.Request.Form.TryGetValue("answer3", out var answer3)
				|| !HttpContext.Request.Form.TryGetValue("selectedCategories", out var selectedCategories)
				|| !int.TryParse(correctAnswerStr, out var correctAnswer))
				return Json(new { error = "invalid_model" });

			var questionModel = new ModifyQuestionModel() { Answer0 = answer0.ToString().Trim(), Answer1 = answer1.ToString().Trim(), Answer2 = answer2.ToString().Trim(), Answer3 = answer3.ToString().Trim(), Label = text.ToString().Trim(), CorrectAnswer = correctAnswer, SelectedCategories = selectedCategories.ToString().Split(',').ToList(), Image = null, Author = account.Id };

			if (!TryValidateModel(questionModel))
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			questionModel.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (questionModel.SelectedCategories.Count == 0)
				return Json(new { error = "invalid_model" });

			string imagePath = null;
			var imageFile = HttpContext.Request.Form.Files.GetFile("image");
			if (imageFile != null)
			{
				var ext = Path.GetExtension(imageFile.FileName);
				if (ext != ".jpg" && ext != ".png")
					return Json(new { error = "invalid_model" });

				var randomName = Path.GetRandomFileName();
				var filePath = Path.Combine("wwwroot", "uploads", randomName + ext);
				var png = new byte[] { 137, 80, 78, 71 };
				var jpeg = new byte[] { 255, 216, 255, 224 };
				var jpeg2 = new byte[] { 255, 216, 255, 225 };

				var buffer = new byte[0x1000];

				using (var fileClientStream = imageFile.OpenReadStream())
				{
					var readCount = await fileClientStream.ReadAsync(buffer, 0, buffer.Length);

					if (!png.SequenceEqual(buffer.Take(png.Length))
						&& !jpeg.SequenceEqual(buffer.Take(jpeg.Length))
						&& !jpeg2.SequenceEqual(buffer.Take(jpeg2.Length)))
					{
						return Json(new { error = "invalid_model" });
					}

					using (var fileServerStream = System.IO.File.Create(filePath))
					{
						while (readCount > 0)
						{
							await fileServerStream.WriteAsync(buffer, 0, readCount);
							readCount = await fileClientStream.ReadAsync(buffer, 0, buffer.Length);
						}
					}
				}

				imagePath = $"/uploads/{randomName + ext}";
			}

			questionModel.Image = imagePath;

			var requests = _databaseService.GetQuestionRequestsCollection();
			var request = new QuestionRequestDTO() { CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Author = account.Id, Image = questionModel.Image, Prority = account.ReportWeight, Answers = new List<string>() { questionModel.Answer0, questionModel.Answer1, questionModel.Answer2, questionModel.Answer3 }, CorrectAnswer = questionModel.CorrectAnswer, Result = QuestionRequestResult.None, Text = questionModel.Label, Categories = questionModel.SelectedCategories };
			await requests.InsertOneAsync(request);

			var accounts = _databaseService.GetAccountsCollection();
			await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Inc(x => x.ActiveReports, 1));

			return Json(new { success = "question_request_added" });
		}
	}
}
