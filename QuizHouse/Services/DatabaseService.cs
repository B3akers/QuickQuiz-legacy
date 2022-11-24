using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizHouse.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
	public class DatabaseService
	{
		private readonly IMongoCollection<CategoryDTO> _categoriesCollection;
		private readonly IMongoCollection<QuestionDTO> _questionsCollection;
		private readonly IMongoCollection<DeviceDTO> _devicesCollection;
		private readonly IMongoCollection<AccountDTO> _accountsCollection;
		private readonly IMongoCollection<EmailConfirmationDTO> _emailConfirmationsCollection;
		private readonly IMongoCollection<PasswordResetDTO> _passwordResetsCollection;
		private readonly IMongoCollection<GameDTO> _gamesCollection;
		private readonly IMongoCollection<QuestionReportDTO> _questionReportsCollection;
		private readonly IMongoCollection<QuestionRequestDTO> _questionRequestsCollection;

		private MongoClient _client;

		public DatabaseService(IConfiguration configuration)
		{
			_client = new MongoClient(configuration["Mongo:ConnectionString"]);
			var mongoDatabase = _client.GetDatabase(configuration["Mongo:DatabaseName"]);

			_categoriesCollection = mongoDatabase.GetCollection<CategoryDTO>("categories");
			_questionsCollection = mongoDatabase.GetCollection<QuestionDTO>("questions");
			_devicesCollection = mongoDatabase.GetCollection<DeviceDTO>("devices");
			_accountsCollection = mongoDatabase.GetCollection<AccountDTO>("accounts");
			_emailConfirmationsCollection = mongoDatabase.GetCollection<EmailConfirmationDTO>("email_confirmations");
			_passwordResetsCollection = mongoDatabase.GetCollection<PasswordResetDTO>("password_resets");
			_gamesCollection = mongoDatabase.GetCollection<GameDTO>("games");
			_questionReportsCollection = mongoDatabase.GetCollection<QuestionReportDTO>("question_reports");
			_questionRequestsCollection = mongoDatabase.GetCollection<QuestionRequestDTO>("question_requests");
		}

		public MongoClient GetMongoClient()
		{
			return _client;
		}

		public IMongoCollection<QuestionRequestDTO> GetQuestionRequestsCollection()
		{
			return _questionRequestsCollection;
		}

		public IMongoCollection<QuestionReportDTO> GetQuestionReportsCollection()
		{
			return _questionReportsCollection;
		}

		public IMongoCollection<GameDTO> GetGamesCollection()
		{
			return _gamesCollection;
		}

		public IMongoCollection<QuestionDTO> GetQuestionsCollection()
		{
			return _questionsCollection;
		}

		public IMongoCollection<PasswordResetDTO> GetPasswordResetsCollection()
		{
			return _passwordResetsCollection;
		}

		public IMongoCollection<EmailConfirmationDTO> GetEmailConfirmationsCollection()
		{
			return _emailConfirmationsCollection;
		}

		public IMongoCollection<CategoryDTO> GetCategoryCollection()
		{
			return _categoriesCollection;
		}

		public IMongoCollection<AccountDTO> GetAccountsCollection()
		{
			return _accountsCollection;
		}

		public IMongoCollection<DeviceDTO> GetDevicesCollection()
		{
			return _devicesCollection;
		}
		public async Task<List<CategoryDTO>> GetCategoriesAsync()
		{
			return await (await _categoriesCollection.FindAsync(Builders<CategoryDTO>.Filter.Empty, new FindOptions<CategoryDTO>() { Sort = Builders<CategoryDTO>.Sort.Descending(x => x.Popularity) })).ToListAsync();
		}

		public async Task<List<CategoryDTO>> GetRandomCategoriesAsync(int number, IEnumerable<string> skip)
		{
			var find = new BsonDocument[] {
				new BsonDocument("$match", new BsonDocument("_id", new BsonDocument("$nin", new BsonArray( skip.Select(x => ObjectId.Parse(x)) )))),
				new BsonDocument("$sample", new BsonDocument("size", number))
			};

			return (await (await _categoriesCollection.AggregateAsync<CategoryDTO>(find)).ToListAsync());
		}

		public async Task<List<CategoryDTO>> GetRandomCategoriesWithQuestionCountAsync(int number, IEnumerable<string> skip, int minimumQuestionCount)
		{
			var find = new BsonDocument[] {
				new BsonDocument("$match", new BsonDocument("$and", new BsonArray{
					new BsonDocument("_id", new BsonDocument("$nin", new BsonArray( skip.Select(x => ObjectId.Parse(x)) ))),
					new BsonDocument("QuestionCount", new BsonDocument("$gte", minimumQuestionCount) )
				})),
				new BsonDocument("$sample", new BsonDocument("size", number))
			};

			return (await (await _categoriesCollection.AggregateAsync<CategoryDTO>(find)).ToListAsync());
		}

		public async Task<List<QuestionDTO>> GetRandomQuestionsFromCategoryAsync(string categoryId, int number, IEnumerable<string> skip)
		{
			var find = new BsonDocument[] {
				new BsonDocument("$match", new BsonDocument("$and", new BsonArray{ new BsonDocument("_id", new BsonDocument("$nin", new BsonArray(skip.Select(x => ObjectId.Parse(x))))), new BsonDocument("Categories", ObjectId.Parse(categoryId)) })),
				new BsonDocument("$sample", new BsonDocument("size", number))
			};

			return (await (await _questionsCollection.AggregateAsync<QuestionDTO>(find)).ToListAsync());
		}

		public async Task<List<QuestionDTO>> GetRandomQuestionsFromCategoriesAsync(IEnumerable<string> skipCategories, int number, IEnumerable<string> skip)
		{
			var find = new BsonDocument[] {
				new BsonDocument("$match", new BsonDocument("$and", new BsonArray{ new BsonDocument("_id", new BsonDocument("$nin", new BsonArray(skip.Select(x => ObjectId.Parse(x))))), new BsonDocument("Categories", new BsonDocument("$nin", new BsonArray(skipCategories.Select(x => ObjectId.Parse(x)))) )})),
				new BsonDocument("$sample", new BsonDocument("size", number))
			};

			return (await (await _questionsCollection.AggregateAsync<QuestionDTO>(find)).ToListAsync());
		}
	}
}
