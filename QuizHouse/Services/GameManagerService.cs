using MongoDB.Driver;
using MongoDB.Driver.Linq;
using QuizHouse.Dto;
using QuizHouse.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class GameManagerService
    {
        private readonly DatabaseService _databaseService;

        private readonly ConcurrentDictionary<string, GameBase> _activeGames = new ConcurrentDictionary<string, GameBase>();

        public GameManagerService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public GameBase GetActiveGame(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
                return null;

            if (_activeGames.TryGetValue(gameId, out var game))
                return game;

            return null;
        }

        public async Task<string> CreateSoloGame(AccountDTO account, string categoryId)
        {
            var questions = await _databaseService.GetRandomQuestionsFromCategoryAsync(categoryId, 6, new HashSet<string>());
            if (questions.Count == 0)
                return string.Empty;

            var gameDTO = new GameDTO();
            gameDTO.Categories = new List<string>();
            gameDTO.Questions = new List<string>();
            gameDTO.Players = new List<string>();
            gameDTO.GameType = GameTypeDTO.Solo;
            gameDTO.GameStatus = GameStatusDTO.Running;
            gameDTO.Players.Add(account.Id);
            gameDTO.Categories.Add(categoryId);
            gameDTO.Questions.AddRange(questions.Select(x => x.Id));

            var games = _databaseService.GamesCollection();
            await games.InsertOneAsync(gameDTO);

            var soloGame = new GameBase() { Id = gameDTO.Id };
            var gamePlayer = new GamePlayerBase() { Id = account.Id };

            soloGame.Players.TryAdd(account.Id, gamePlayer);
            _activeGames.TryAdd(gameDTO.Id, soloGame);

            var accounts = _databaseService.GetAccountsCollection();
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.LastGameId, gameDTO.Id));

            return gameDTO.Id;
        }
    }
}
