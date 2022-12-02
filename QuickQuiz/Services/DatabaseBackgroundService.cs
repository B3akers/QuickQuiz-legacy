using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using MongoDB.Driver;
using SharpCompress.Common;
using QuickQuiz.Dto;

namespace QuickQuiz.Services
{
	public class DatabaseBackgroundService : BackgroundService
	{
		private DatabaseService _quizService;
		public DatabaseBackgroundService(DatabaseService quizService)
		{
			_quizService = quizService;
		}

		protected async override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var accounts = _quizService.GetAccountsCollection();
			var emailConfirmations = _quizService.GetEmailConfirmationsCollection();
			var passwordResets = _quizService.GetPasswordResetsCollection();

			while (!stoppingToken.IsCancellationRequested)
			{
				var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				await accounts.DeleteManyAsync(x => !x.EmailConfirmed && x.CreationTime <= currentTime - (3600 * 24 * 2));
				await emailConfirmations.DeleteManyAsync(x => x.CreationTime <= currentTime - (3600 * 24));
				await passwordResets.DeleteManyAsync(x => x.CreationTime <= currentTime - 3600);
				await Task.Delay(1000 * 3600, stoppingToken);
			}
		}
	}
}
