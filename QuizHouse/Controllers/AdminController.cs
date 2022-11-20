using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizHouse.ActionFilters;
using QuizHouse.Dto;
using QuizHouse.Models;
using QuizHouse.Services;
using QuizHouse.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace QuizHouse.Controllers
{
	public class ModifyCategoryParamters
	{
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(40)]
		public string Label { get; set; }

		[Required]
		[MinLength(6)]
		[MaxLength(8)]
		public string Color { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(80)]
		public string Icon { get; set; }
	}

	public class ModifyQuestionParamters
	{
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(350)]
		public string Label { get; set; }

		[MaxLength(80)]
		public string Image { get; set; }

		[RegularExpression("^[a-f\\d]{24}$")]
		public string Author { get; set; }

		[Required]
		public int CorrectAnswer { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer0 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer1 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer2 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer3 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(8)]
		public List<string> SelectedCategories { get; set; }
	};

	public class DeleteRecordParamters
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }
	}

	[TypeFilter(typeof(AdminActionFilter))]
	public class AdminController : Controller
	{
		private readonly DatabaseService _databaseService;
		public AdminController(DatabaseService databaseService)
		{
			_databaseService = databaseService;
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

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteCategory([FromBody] DeleteRecordParamters paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var catagories = _databaseService.GetCategoryCollection();
			await catagories.DeleteOneAsync(x => x.Id == paramters.Id);

			return Json(new { success = "record_deleted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteQuestion([FromBody] DeleteRecordParamters paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var questions = _databaseService.GetQuestionsCollection();
			await questions.DeleteOneAsync(x => x.Id == paramters.Id);

			return Json(new { success = "record_deleted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditCategory([FromBody] ModifyCategoryParamters paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
				return Json(new { error = "invalid_model" });

			var catagories = _databaseService.GetCategoryCollection();
			await catagories.UpdateOneAsync(x => x.Id == paramters.Id, Builders<CategoryDTO>.Update.Set(x => x.Label, paramters.Label).Set(x => x.Color, paramters.Color).Set(x => x.Icon, paramters.Icon));

			return Json(new { success = "category_edited" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditQuestion([FromBody] ModifyQuestionParamters paramters)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
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
			await questions.UpdateOneAsync(x => x.Id == paramters.Id, Builders<QuestionDTO>.Update.Set(x => x.Text, paramters.Label).Set(x => x.Image, paramters.Image).Set(x => x.Author, paramters.Author).Set(x => x.CorrectAnswer, paramters.CorrectAnswer).Set(x => x.Answers, new List<string>() { paramters.Answer0, paramters.Answer1, paramters.Answer2, paramters.Answer3 }).Set(x => x.Categories, paramters.SelectedCategories));

			return Json(new { success = "question_edited" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddQuestion([FromBody] ModifyQuestionParamters paramters)
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
		public async Task<IActionResult> AddCategory([FromBody] ModifyCategoryParamters paramters)
		{
			if (!ModelState.IsValid)
				return Json(new { error = "invalid_model" });

			var category = new CategoryDTO() { Color = paramters.Color, Label = paramters.Label, Icon = paramters.Icon };
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
		public async Task<IActionResult> GetQuestions([FromBody] DataTableProcessParametrs parametrs)
		{
			var questions = _databaseService.GetQuestionsCollection();

			var datatableDef = Datatables.StartPaging<QuestionDTO>(parametrs).SetGlobalFilterField(x => x.Id).AllowFilterFor(x => x.Categories).ApplyColumnFilter().ApplyGlobalFilter().SortDescendingById();
			var result = await datatableDef.Execute(questions);

			return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1 });

		}

		[HttpPost]
		public async Task<IActionResult> GetCategories([FromBody] DataTableProcessParametrs parametrs)
		{
			var categories = _databaseService.GetCategoryCollection();

			var datatableDef = Datatables.StartPaging<CategoryDTO>(parametrs).SetGlobalFilterField(x => x.Label).ApplyGlobalFilter().ApplySort().SortDescendingById();
			var result = await datatableDef.Execute(categories);

			return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1 });
		}
	}
}