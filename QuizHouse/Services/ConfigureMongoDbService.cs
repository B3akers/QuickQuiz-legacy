using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using QuizHouse.Dto;
using QuizHouse.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuizHouse.Utility;
using System.Drawing;

namespace QuizHouse.Services
{
	public class ConfigureMongoDbService : IHostedService
	{
		private readonly DatabaseService _quizService;
		private readonly IConfiguration _configuration;

		public ConfigureMongoDbService(IConfiguration configuration, DatabaseService quizService) => (_configuration, _quizService) = (configuration, quizService);

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			BsonClassMap.RegisterClassMap<GameBase>(cm => {
				cm.SetDiscriminatorIsRequired(false);
				cm.SetIsRootClass(false);
			});

			BsonClassMap.RegisterClassMap<GamePlayerBase>(cm => {
				cm.SetDiscriminatorIsRequired(false);
				cm.SetIsRootClass(false);
			});

			BsonSerializer.RegisterDiscriminatorConvention(typeof(GameBase), NullDiscriminatorConvention.Instance);
			BsonSerializer.RegisterDiscriminatorConvention(typeof(GamePlayerBase), NullDiscriminatorConvention.Instance);

			var client = _quizService.GetMongoClient();
			var mongoDatabase = client.GetDatabase(_configuration["Mongo:DatabaseName"]);

			var categories = mongoDatabase.GetCollection<Dto.CategoryDTO>("categories");
			await categories.Indexes.CreateOneAsync(new CreateIndexModel<Dto.CategoryDTO>(Builders<Dto.CategoryDTO>.IndexKeys.Ascending(x => x.QuestionCount)));

			var questions = mongoDatabase.GetCollection<Dto.QuestionDTO>("questions");
			await questions.Indexes.CreateOneAsync(new CreateIndexModel<Dto.QuestionDTO>(Builders<Dto.QuestionDTO>.IndexKeys.Ascending(x => x.Categories)));

			var devices = mongoDatabase.GetCollection<Dto.DeviceDTO>("devices");
			await devices.Indexes.CreateOneAsync(new CreateIndexModel<Dto.DeviceDTO>(Builders<Dto.DeviceDTO>.IndexKeys.Ascending(x => x.AccountId)));
			await devices.Indexes.CreateOneAsync(new CreateIndexModel<Dto.DeviceDTO>(Builders<Dto.DeviceDTO>.IndexKeys.Ascending(x => x.Key), new CreateIndexOptions() { Unique = true }));

			var accounts = mongoDatabase.GetCollection<Dto.AccountDTO>("accounts");
			await accounts.Indexes.CreateOneAsync(new CreateIndexModel<Dto.AccountDTO>(Builders<Dto.AccountDTO>.IndexKeys.Ascending(x => x.Email), new CreateIndexOptions() { Unique = true, Collation = new Collation("en", strength: CollationStrength.Secondary) }));
			await accounts.Indexes.CreateOneAsync(new CreateIndexModel<Dto.AccountDTO>(Builders<Dto.AccountDTO>.IndexKeys.Ascending(x => x.Username), new CreateIndexOptions() { Collation = new Collation("en", strength: CollationStrength.Secondary) }));
			await accounts.Indexes.CreateOneAsync(new CreateIndexModel<Dto.AccountDTO>(Builders<Dto.AccountDTO>.IndexKeys.Ascending(x => x.CreationTime)));
			await accounts.Indexes.CreateOneAsync(new CreateIndexModel<Dto.AccountDTO>(Builders<Dto.AccountDTO>.IndexKeys.Ascending(x => x.EmailConfirmed)));

			var email_confirmations = mongoDatabase.GetCollection<Dto.EmailConfirmationDTO>("email_confirmations");
			await email_confirmations.Indexes.CreateOneAsync(new CreateIndexModel<Dto.EmailConfirmationDTO>(Builders<Dto.EmailConfirmationDTO>.IndexKeys.Ascending(x => x.Key), new CreateIndexOptions() { Unique = true }));

			var password_resets = mongoDatabase.GetCollection<Dto.PasswordResetDTO>("password_resets");
			await password_resets.Indexes.CreateOneAsync(new CreateIndexModel<Dto.PasswordResetDTO>(Builders<Dto.PasswordResetDTO>.IndexKeys.Ascending(x => x.Key), new CreateIndexOptions() { Unique = true }));

			var games = mongoDatabase.GetCollection<Dto.GameDTO>("games");
			await games.UpdateManyAsync(x => x.GameStatus == GameStatusDTO.Running, Builders<GameDTO>.Update.Set(x => x.GameStatus, GameStatusDTO.Aborted));
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
