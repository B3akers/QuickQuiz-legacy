using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class ConfigureMongoDbIndexesService : IHostedService
    {
        private readonly QuizService _quizService;
        private readonly IConfiguration _configuration;

        public ConfigureMongoDbIndexesService(IConfiguration configuration, QuizService quizService) => (_configuration, _quizService) = (configuration, quizService);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
