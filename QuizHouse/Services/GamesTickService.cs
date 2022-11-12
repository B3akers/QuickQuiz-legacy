using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
	public class GamesTickService : BackgroundService
	{
		private GamesService _gamesService;
		public GamesTickService(GamesService gamesService)
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
