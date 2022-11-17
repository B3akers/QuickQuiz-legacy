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
		private readonly IMongoCollection<AnswerDTO> _answersCollection;
		private readonly IMongoCollection<QuestionDTO> _questionsCollection;
		private readonly IMongoCollection<DeviceDTO> _devicesCollection;
		private readonly IMongoCollection<AccountDTO> _accountsCollection;
		private readonly IMongoCollection<EmailConfirmationDTO> _emailConfirmationsCollection;
		private readonly IMongoCollection<PasswordResetDTO> _passwordResetsCollection;
        private readonly IMongoCollection<GameDTO> _gamesCollection;

        private MongoClient _client;

		public DatabaseService(IConfiguration configuration)
		{
			_client = new MongoClient(configuration["Mongo:ConnectionString"]);
			var mongoDatabase = _client.GetDatabase(configuration["Mongo:DatabaseName"]);

			_categoriesCollection = mongoDatabase.GetCollection<CategoryDTO>("categories");
			_answersCollection = mongoDatabase.GetCollection<AnswerDTO>("answers");
			_questionsCollection = mongoDatabase.GetCollection<QuestionDTO>("questions");
			_devicesCollection = mongoDatabase.GetCollection<DeviceDTO>("devices");
			_accountsCollection = mongoDatabase.GetCollection<AccountDTO>("accounts");
			_emailConfirmationsCollection = mongoDatabase.GetCollection<EmailConfirmationDTO>("email_confirmations");
			_passwordResetsCollection = mongoDatabase.GetCollection<PasswordResetDTO>("password_resets");
            _gamesCollection = mongoDatabase.GetCollection<GameDTO>("games");
        }

        public MongoClient GetMongoClient()
		{
			return _client;
		}

        public IMongoCollection<GameDTO> GamesCollection()
        {
            return _gamesCollection;
        }

        public IMongoCollection<PasswordResetDTO> PasswordResetsCollection()
		{
			return _passwordResetsCollection;
		}

		public IMongoCollection<EmailConfirmationDTO> EmailConfirmationsCollection()
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
			return await (await _categoriesCollection.FindAsync(x => true)).ToListAsync();
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

		public async Task<List<AnswerDTO>> GetAnswersAsync(IEnumerable<string> answersIds)
		{
			var find = new BsonDocument[] {
				new BsonDocument("$match", new BsonDocument("_id", new BsonDocument("$in", new BsonArray( answersIds.Select(x => ObjectId.Parse(x)) ))))
			};

			return (await (await _answersCollection.AggregateAsync<AnswerDTO>(find)).ToListAsync());
		}
	}
}
