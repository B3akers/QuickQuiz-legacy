using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickQuiz.Services
{
	public class GamesTickServiceOld : BackgroundService
	{
		private GamesServiceOld _gamesService;
		public GamesTickServiceOld(GamesServiceOld gamesService)
		{
			_gamesService = gamesService;
		}

		protected async override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);

				await _gamesService.Tick();
			}
		}
	}
}
