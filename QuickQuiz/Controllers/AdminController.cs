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
        public async Task<IActionResult> EditCategory([FromBody] ModifyCategoryModel paramters)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.Id))
                return Json(new { error = "invalid_model" });

            var builderSet = Builders<CategoryDTO>.Update.Set(x => x.Label, paramters.Label).Set(x => x.Color, paramters.Color);
            var catagories = _databaseService.GetCategoryCollection();

            if (!string.IsNullOrEmpty(paramters.IconBase64))
            {
                var uploadedIconPath = await _cdnUploader.UploadBase64(paramters.IconBase64);
                if (string.IsNullOrEmpty(uploadedIconPath))
                    return Json(new { error = "invalid_model" });

                var category = await (await catagories.FindAsync(x => x.Id == paramters.Id)).FirstOrDefaultAsync();
                if (category != null)
                {
                    await _cdnUploader.DeleteFile(category.Icon);
                    builderSet = builderSet.Set(x => x.Icon, uploadedIconPath);
                }
            }

            await catagories.UpdateOneAsync(x => x.Id == paramters.Id, builderSet);

            return Json(new { success = "category_edited" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([FromBody] ModifyCategoryModel paramters)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(paramters.IconBase64))
                return Json(new { error = "invalid_model" });

            var uploadedIconPath = await _cdnUploader.UploadBase64(paramters.IconBase64);
            if (string.IsNullOrEmpty(uploadedIconPath))
                return Json(new { error = "invalid_model" });
            
            var category = new CategoryDTO() { Color = paramters.Color, Label = paramters.Label, Icon = uploadedIconPath };
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
        public async Task<IActionResult> GetCategories([FromBody] DataTableProcessParametrs parametrs)
        {
            var categories = _databaseService.GetCategoryCollection();

            var datatableDef = Datatables.StartPaging<CategoryDTO>(parametrs).AddGlobalFilterField(x => x.Label, true).ApplyGlobalFilter().ApplySort().SortDescendingById();
            var result = await datatableDef.Execute(categories);

            return Json(new { parametrs.Draw, RecordsFiltered = result.Item2, RecordsTotal = result.Item3, data = result.Item1 });
        }
    }
}