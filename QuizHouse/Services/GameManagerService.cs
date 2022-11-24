using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuizHouse.Dto;
using QuizHouse.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
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

		public bool TryAssignSocketToPlayer(string gameId, string accountId, WebSocket socket)
		{
			if (!_activeGames.TryGetValue(gameId, out var gameBase))
				return false;

			return gameBase.TryAssignSocketToPlayer(accountId, socket);
		}

		public bool InvalidateSocket(string gameId, string accountId, WebSocketState state)
		{
			if (!_activeGames.TryGetValue(gameId, out var gameBase))
				return false;

			return gameBase.InvalidateSocket(accountId, state);
		}

		public async Task<string> CreateSoloGame(AccountDTO account, string categoryId)
		{
			var questions = await _databaseService.GetRandomQuestionsFromCategoryAsync(categoryId, 6, Enumerable.Empty<string>());
			if (questions.Count == 0)
				return string.Empty;

			var gamePlayer = new GamePlayerBase();
			gamePlayer.Id = account.Id;
			gamePlayer.Username = account.Username;
			gamePlayer.TimeoutTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + GameConstSettings.TIMEOUT_LOBBY_UNKNOWN;
			foreach (var connection in account.Connections)
			{
				if (connection.Type == "Twitch")
				{
					var twitchConnection = (TwitchConnectionDTO)connection;
					gamePlayer.ConnectionsInfo.Add(new Tuple<string, string, string>(connection.Type, twitchConnection.Login, twitchConnection.Displayname));
				}
			}
			gamePlayer.CategoryVoteId = categoryId;

			var gameBase = new GameBase();
			gameBase.Categories = new List<string>();
			gameBase.Questions = new List<string>();
			gameBase.Players = new List<GamePlayerDTO>();
			gameBase.CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			gameBase.GameType = GameTypeDTO.Solo;
			gameBase.CurrentGameState = GameBase.GameState.None;
			gameBase.GameStatus = GameStatusDTO.Running;
			gameBase.AddPlayer(gamePlayer);

			var games = _databaseService.GetGamesCollection();
			await games.InsertOneAsync(gameBase);

			_activeGames.TryAdd(gameBase.Id, gameBase);

			var accounts = _databaseService.GetAccountsCollection();
			await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.LastGameId, gameBase.Id));

			await gameBase.ChangeToQuestionsStage(categoryId, questions);

			return gameBase.Id;
		}

		public async Task GameTick()
		{
			ConcurrentBag<string> gamesAborted = new ConcurrentBag<string>();
			List<Task> _gameTickTasks = new List<Task>();
			foreach (var game in _activeGames)
				_gameTickTasks.Add(Task.Run(async () =>
				{
					if (await game.Value.Tick(_databaseService) == false)
						gamesAborted.Add(game.Value.Id);
				}));

			await Task.WhenAll(_gameTickTasks);

			var games = _databaseService.GetGamesCollection();
			foreach (var abortedGameId in gamesAborted)
			{
				if (_activeGames.TryRemove(abortedGameId, out var gameBase))
				{
					await gameBase.FinishGame(gameBase.GameStatus != GameStatusDTO.Finished);
					await games.ReplaceOneAsync(x => x.Id == gameBase.Id, gameBase);

					var categories = _databaseService.GetCategoryCollection();
					await categories.UpdateManyAsync(Builders<CategoryDTO>.Filter.In(x => x.Id, gameBase.Categories), Builders<CategoryDTO>.Update.Inc(x => x.Popularity, 1ul));
				}
			}
		}
	}
}