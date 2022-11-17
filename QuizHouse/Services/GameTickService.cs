using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;

namespace QuizHouse.Services
{
    public class GameTickService : BackgroundService
    {
        private GameManagerService _gameManagerService;
        public GameTickService(GameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                await _gameManagerService.GameTick();
            }
        }
    }
}
