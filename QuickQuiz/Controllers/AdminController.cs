using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using QuickQuiz.ActionFilters;
using QuickQuiz.Dto;
using QuickQuiz.Interfaces;
using QuickQuiz.Models;
using QuickQuiz.Services;
using QuickQuiz.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace QuickQuiz.Controllers
{
	[TypeFilter(typeof(AdminActionFilter))]
	public class AdminController : Controller
	{
		private readonly DatabaseService _databaseService;
		private readonly IAccountRepository _accountRepository;
		private readonly ICdnUploader _cdnUploader;

		public AdminController(DatabaseService databaseService, ICdnUploader cdnUploader, IAccountRepository accountRepository)
		{
			_databaseService = databaseService;
			_cdnUploader = cdnUploader;
			_accountRepository = accountRepository;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Categories()
		{
			return View();
		}

		public IActionResult Questions()
		{
			return View();
		}

		public IActionResult Accounts()
		{
			return View();
		}

		[HttpGet("Admin/Profile/{profileId}")]
		public async Task<IActionResult> Profile(string profileId)
		{
			var model = new AdminProfileModel();
			model.Account = await _accountRepository.GetAccount(profileId);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ModifyUserProfile([FromBody] ModifyUserProfilModel parametrs)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var userAccount = await _accountRepository.GetAccount(parametrs.Id);
			if (userAccount == null)
				return Json(new { error = "user_not_found" });

			var builder = Builders<AccountDTO>.Update;
			var accounts = _databaseService.GetAccountsCollection();
			List<UpdateDefinition<AccountDTO>> updates = new List<UpdateDefinition<AccountDTO>>();

			if (!string.IsNullOrEmpty(parametrs.UserColor))
				updates.Add(builder.Set(x => x.UserColor, parametrs.UserColor));

			if (!string.IsNullOrEmpty(parametrs.CustomColor))
				updates.Add(builder.Set(x => x.CustomColor, parametrs.CustomColor == "#000000" ? null : parametrs.CustomColor));

			if (!string.IsNullOrEmpty(parametrs.UserEmail))
				updates.Add(builder.Set(x => x.Email, parametrs.UserEmail));

			if (!string.IsNullOrEmpty(parametrs.UserName))
				updates.Add(builder.Set(x => x.Username, parametrs.UserName));

			if (parametrs.IsAdmin.HasValue)
				updates.Add(builder.Set(x => x.IsAdmin, parametrs.IsAdmin.Value));

			if (parametrs.IsModerator.HasValue)
				updates.Add(builder.Set(x => x.IsModerator, parametrs.IsModerator.Value));

			if (parametrs.StreamerMode.HasValue)
				updates.Add(builder.Set(x => x.StreamerMode, parametrs.StreamerMode.Value));

			if (parametrs.PrivateProfile.HasValue)
				updates.Add(builder.Set(x => x.ProfilPrivate, parametrs.PrivateProfile.Value));

			if (parametrs.EmailConfirmed.HasValue)
				updates.Add(builder.Set(x => x.EmailConfirmed, parametrs.EmailConfirmed.Value));

			if (parametrs.ReportWeight.HasValue)
				updates.Add(builder.Set(x => x.ReportWeight, parametrs.ReportWeight.Value));

			await accounts.UpdateOneAsync(x => x.Id == userAccount.Id, builder.Combine(updates));

			return Json(new { success = "user_modified" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteCategory([FromBody] DeleteRecordModel paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var catagories = _databaseService.GetCategoryCollection();
			await catagories.DeleteOneAsync(x => x.Id == paramters.Id);

			return Json(new { success = "record_deleted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteQuestion([FromBody] DeleteRecordModel paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var questions = _databaseService.GetQuestionsCollection();
			await questions.DeleteOneAsync(x => x.Id == paramters.Id);

			return Json(new { success = "record_deleted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditCategory([FromBody] ModifyCategoryModel paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var catagories = _databaseService.GetCategoryCollection();
			await catagories.UpdateOneAsync(x => x.Id == paramters.Id, Builders<CategoryDTO>.Update.Set(x => x.Label, paramters.Label).Set(x => x.Color, paramters.Color).Set(x => x.Icon, paramters.Icon));

			return Json(new { success = "category_edited" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditQuestion([FromBody] ModifyQuestionModel paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			paramters.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (string.IsNullOrEmpty(paramters.Author))
				paramters.Author = null;

			if (string.IsNullOrEmpty(paramters.Image))
				paramters.Image = null;

			var questions = _databaseService.GetQuestionsCollection();
			await questions.UpdateOneAsync(x => x.Id == paramters.Id, Builders<QuestionDTO>.Update.Set(x => x.Text, paramters.Label).Set(x => x.Image, paramters.Image).Set(x => x.Author, paramters.Author).Set(x => x.CorrectAnswer, paramters.CorrectAnswer).Set(x => x.Answers, new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }).Set(x => x.Categories, paramters.SelectedCategories));

			return Json(new { success = "question_edited" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddQuestion([FromBody] ModifyQuestionModel paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var categories = await _databaseService.GetCategoriesAsync();
			paramters.SelectedCategories.RemoveAll(x => categories.FindIndex(y => y.Id == x) == -1);

			if (paramters.SelectedCategories.Count == 0)
				return Json(new { error = "invalid_model" });

			if (string.IsNullOrEmpty(paramters.Author))
				paramters.Author = null;

			if (string.IsNullOrEmpty(paramters.Image))
				paramters.Image = null;

			var questions = _databaseService.GetQuestionsCollection();
			var question = new QuestionDTO() { Text = paramters.Label, Author = paramters.Author, Image = paramters.Image, CorrectAnswer = paramters.CorrectAnswer, Answers = new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }, Categories = paramters.SelectedCategories };

			await questions.InsertOneAsync(question);

			return Json(new { success = "question_added" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddCategory([FromBody] ModifyCategoryModel paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.IconBase64))
				return Json(new { error = "invalid_model" });

			var startIndex = paramters.IconBase64.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
			if (startIndex == -1)
				return Json(new { error = "invalid_model" });

			var ext = "." + paramters.IconBase64.Substring(paramters.IconBase64.IndexOf('/') + 1, 3);
			paramters.IconBase64 = paramters.IconBase64.Substring(startIndex + 7);

			var randomName = Path.GetRandomFileName();
			var filePath = Path.Combine("wwwroot", "uploads", randomName + ext);

			await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(paramters.IconBase64));
			var icon = await _cdnUploader.UploadFileAsync($"/uploads/{randomName + ext}");

			var category = new CategoryDTO() { Color = paramters.Color, Label = paramters.Label, Icon = icon };
			var catagories = _databaseService.GetCategoryCollection();

			await catagories.InsertOneAsync(category);

			return Json(new { success = "category_added" });
		}

		[HttpGet]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CategoriesResetPopularity()
		{
			var categories = _databaseService.GetCategoryCollection();
			await categories.UpdateManyAsync(Builders<CategoryDTO>.Filter.Empty, Builders<CategoryDTO>.Update.Set(x => x.Popularity, 0ul));

			return Json(new { success = "refresh_success" });
		}

		[HttpGet]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CategoriesRefreshQuestionsCount()
		{
			var categoriesQuestions = new ConcurrentDictionary<string, int>();
			var empty = Builders<QuestionDTO>.Filter.Empty;
			var questions = _databaseService.GetQuestionsCollection();
			await (await questions.FindAsync(empty)).ForEachAsync(x =>
			{
				foreach (var categoryId in x.Categories)
					categoriesQuestions.AddOrUpdate(categoryId, 1, (key, oldValue) => oldValue + 1);
			});

			List<UpdateOneModel<CategoryDTO>> bulk = new List<UpdateOneModel<CategoryDTO>>();
			foreach (var info in categoriesQuestions)
				bulk.Add(new UpdateOneModel<CategoryDTO>(Builders<CategoryDTO>.Filter.Eq(x => x.Id, info.Key), Builders<CategoryDTO>.Update.Set(x => x.QuestionCount, info.Value)));

			var categories = _databaseService.GetCategoryCollection();
			await categories.BulkWriteAsync(bulk);

			return Json(new { success = "refresh_success" });
		}

		[HttpPost]
		public async Task<IActionResult> GetAccounts([FromBody] DataTableProcessParametrs parametrs)
		{
			var accounts = _databaseService.GetAccountsCollection();

			var datatableDef = Datatables.StartPaging<AccountDTO>(parametrs).AddGlobalFilterField(x => x.Username, false).AddGlobalFilterField(x => x.Email, false).IgnoreCaseSerach().ApplyGlobalFilter().SortDescendingById();
			var result = await datatableDef.Execute(accounts);

			return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1.Select(x => new { x.Id, x.Username, x.Email, x.CreationTime, x.IsAdmin, x.IsModerator }) });
		}

		[HttpPost]
		public async Task<IActionResult> GetQuestions([FromBody] DataTableProcessParametrs parametrs)
		{
			var questions = _databaseService.GetQuestionsCollection();

			var datatableDef = Datatables.StartPaging<QuestionDTO>(parametrs).AddGlobalFilterField(x => x.Id, false).AllowFilterFor(x => x.Categories).ApplyColumnFilter().ApplyGlobalFilter().SortDescendingById();
			var result = await datatableDef.Execute(questions);

			return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1 });

		}

		[HttpPost]
		public async Task<IActionResult> GetCategories([FromBody] DataTableProcessParametrs parametrs)
		{
			var categories = _databaseService.GetCategoryCollection();

			var datatableDef = Datatables.StartPaging<CategoryDTO>(parametrs).AddGlobalFilterField(x => x.Label, true).ApplyGlobalFilter().ApplySort().SortDescendingById();
			var result = await datatableDef.Execute(categories);

			return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1 });
		}
	}
}