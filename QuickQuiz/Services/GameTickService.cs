using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;

namespace QuickQuiz.Services
{
    public class GameTickService : BackgroundService
    {
        private GameManagerService _gameManagerService;
		private LobbyManagerService _lobbyManagerService;
		public GameTickService(GameManagerService gameManagerService, LobbyManagerService lobbyManagerService)
        {
            _gameManagerService = gameManagerService;
            _lobbyManagerService = lobbyManagerService;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _gameManagerService.GameTick();
                await _lobbyManagerService.LobbyTick();
				await Task.Delay(1000, stoppingToken);
			}
		}
    }
}
