using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuizHouse.Utility;
using QuizHouse.WebSockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
	public class QuizGamePlayer
	{
		public bool IsReady { get; set; }
		public bool IsOwner { get; set; }
		public string AnswerId { get; set; }
		public string CategoryVoteId { get; set; }
		public List<bool> AnswersStatus { get; set; }
		public double Points { get; set; }
		public bool Disconnected { get; set; }
		public long LastPingTime { get; set; }
	}

	public enum QuizGameState
	{
		Lobby,
		CategorySelection,
		PrepareForQuestions,
		QuestionAnswering,
		QuestionAnswered,
		RoundEnd
	}

	public enum QuizGameMode
	{
		Normal,
		AllRandom
	}

	public enum QuizLobbyMode
	{
		Normal,
		TwitchAuth,
		Max
	}

	public class QuizGame
	{
		public string OwnerName { get; set; }
		public QuizGameState GameState { get; set; }
		public QuizLobbyMode LobbyMode { get; set; }
		public QuizGameMode GameMode { get; set; }
		public long CreateTime { get; set; }
		public long CategoryVoteTime { get; set; }
		public long PrepareForQuestionsTime { get; set; }
		public long QuestionAnsweringTime { get; set; }
		public string CurrentCategory { get; set; }
		public ConcurrentDictionary<string, int> CurrentVoteCategories { get; set; }
		public HashSet<string> AcknowledgedCategories { get; set; }
		public HashSet<string> AcknowledgedQuestions { get; set; }
		public ConcurrentDictionary<string, QuizGamePlayer> CurrentPlayers { get; set; }
		public List<Dto.QuestionDTO> CurrentQuestions { get; set; }
		public List<Dto.AnswerDTO> CurrentQuestionsAnswers { get; set; }
		public int CurrentQuestionIndex { get; set; }
		public int MaxCategoriesCount { get; set; }
		public int QuestionsPerCategoryCount { get; set; }
		public int TimeSecondForQuestion { get; set; }
		public int CategoryPerSelectionCount { get; set; }
		public List<string> ExcludedCategoriesList { get; set; }
	}

	public class GamesServiceOld
	{
		private ConcurrentDictionary<string, QuizGame> _games = new ConcurrentDictionary<string, QuizGame>(StringComparer.OrdinalIgnoreCase);

		private ConnectionManagerOld _webSocketConnectionManager;
		private DatabaseService _quizService;

		private JsonSerializerSettings _jsonSerializerSettings;

		public GamesServiceOld(ConnectionManagerOld webSocketConnectionManager, DatabaseService quizService)
		{
			_webSocketConnectionManager = webSocketConnectionManager;
			_quizService = quizService;

			_jsonSerializerSettings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
		}

		public QuizGame FindGame(string gameId)
		{
			if (_games.TryGetValue(gameId, out var game))
				return game;

			return null;
		}

		public bool RemoveGame(string gameId)
		{
			return _games.TryRemove(gameId, out _);
		}

		public Tuple<string, QuizGame> CreateGame()
		{
			var quizGame = new QuizGame()
			{
				CurrentPlayers = new ConcurrentDictionary<string, QuizGamePlayer>(StringComparer.OrdinalIgnoreCase),
				GameState = QuizGameState.Lobby,
				CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				AcknowledgedCategories = new HashSet<string>(),
				AcknowledgedQuestions = new HashSet<string>(),
				CurrentVoteCategories = new ConcurrentDictionary<string, int>(),
				CurrentQuestionsAnswers = new List<Dto.AnswerDTO>(),
				MaxCategoriesCount = 7,
				QuestionsPerCategoryCount = 6,
				TimeSecondForQuestion = 15,
				CategoryPerSelectionCount = 9,
				GameMode = QuizGameMode.Normal,
				LobbyMode = QuizLobbyMode.Normal,
				ExcludedCategoriesList = new List<string>()
			};

			int tries = 0;

			while (tries++ < 10)
			{
				var gameId = Randomizer.RandomReadableString(6);

				if (_games.TryAdd(gameId, quizGame))
					return new Tuple<string, QuizGame>(gameId, quizGame);
			}

			return null;
		}
		public async Task PrepareCategoryVote(string gameId)
		{
			if (!_games.TryGetValue(gameId, out var game))
				return;

			foreach (var player in game.CurrentPlayers)
			{
				player.Value.CategoryVoteId = string.Empty;
				player.Value.AnswersStatus = new List<bool>();
			}

			game.CurrentVoteCategories = new ConcurrentDictionary<string, int>();

			if (game.GameMode == QuizGameMode.Normal)
			{
				var excludedCategories = new HashSet<string>();

				foreach (var category in game.ExcludedCategoriesList)
					excludedCategories.Add(category);

				foreach (var category in game.AcknowledgedCategories)
					excludedCategories.Add(category);

				foreach (var category in (await _quizService.GetRandomCategoriesWithQuestionCountAsync(game.CategoryPerSelectionCount, excludedCategories, 200)))
					game.CurrentVoteCategories.TryAdd(category.Id, 0);
			}

			if (game.CurrentVoteCategories.IsEmpty)
			{
				foreach (var category in (await _quizService.GetRandomCategoriesWithQuestionCountAsync(game.CategoryPerSelectionCount, new HashSet<string>(), 200)))
					game.CurrentVoteCategories.TryAdd(category.Id, 0);
			}

			game.CategoryVoteTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			game.GameState = QuizGameState.CategorySelection;

			await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
			{
				Type = "category_vote_start",
				Value = new
				{
					Categories = game.CurrentVoteCategories.Keys,
					CurrentCategoryIndex = game.AcknowledgedCategories.Count,
					MaxCategoryIndex = game.MaxCategoriesCount,
					CategoryStartTime = game.CategoryVoteTime,
					CurrentCategoryVote = string.Empty,
					PlayersRankings = game.CurrentPlayers.Select(x => new { PlayerName = x.Key, Points = x.Value.Points })
				}
			}, _jsonSerializerSettings)), gameId);
		}
		private async Task GameUpdateTick(KeyValuePair<string, QuizGame> game, ConcurrentBag<string> gamesEnded)
		{
			try
			{
				var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

				if (game.Value.CurrentPlayers.Count <= 0)
				{
					gamesEnded.Add(game.Key);
					return;
				}

				List<string> playersToRemove = new List<string>();

				foreach (var player in game.Value.CurrentPlayers)
				{
					if (!player.Value.Disconnected)
						continue;

					if (currentTime - player.Value.LastPingTime > 45000) //45s
						playersToRemove.Add(player.Key);
				}

				foreach (var removedPlayer in playersToRemove)
				{
					if (game.Value.CurrentPlayers.TryRemove(removedPlayer, out var gamePlayer))
					{
						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "player_leave",
							Value = new
							{
								PlayerName = removedPlayer
							}
						}, _jsonSerializerSettings)), game.Key, removedPlayer);

						if (gamePlayer.IsOwner)
						{
							if (game.Value.CurrentPlayers.Count > 0)
							{
								var newOwner = game.Value.CurrentPlayers.FirstOrDefault();
								newOwner.Value.IsOwner = true;
								game.Value.OwnerName = newOwner.Key;

								foreach (var player in game.Value.CurrentPlayers)
									player.Value.IsReady = false;

								await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								{
									Type = "owner_transfer",
									Value = new
									{
										PlayerName = newOwner.Key
									}
								}, _jsonSerializerSettings)), game.Key, removedPlayer);
							}
						}
					}
				}

				if (game.Value.GameState == QuizGameState.Lobby)
				{
					if (currentTime - game.Value.CreateTime > 600000) //10 mins
					{
						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "game_timeout",
							Value = (string)null
						}, _jsonSerializerSettings)), game.Key);

						gamesEnded.Add(game.Key);
					}

					var gameReadyToStart = false;

					if (game.Value.CurrentPlayers.TryGetValue(game.Value.OwnerName, out var ownerPlayer))
						gameReadyToStart = ownerPlayer.IsReady;

					if (gameReadyToStart)
						await PrepareCategoryVote(game.Key);

					return;
				}

				if (game.Value.GameState == QuizGameState.CategorySelection)
				{
					if (currentTime - game.Value.CategoryVoteTime > (game.Value.GameMode == QuizGameMode.Normal ? (game.Value.TimeSecondForQuestion * 1000 + 1500) : 1500)
						|| !game.Value.CurrentPlayers.Values.Any(x => string.IsNullOrEmpty(x.CategoryVoteId)))
					{
						if (game.Value.GameMode == QuizGameMode.Normal)
						{
							var bestCategoryList = new List<string>();
							var bestCategoryVotes = -1;

							foreach (var category in game.Value.CurrentVoteCategories)
							{
								if (category.Value > bestCategoryVotes)
								{
									bestCategoryVotes = category.Value;
									bestCategoryList.Clear();
									bestCategoryList.Add(category.Key);
								}
								else if (category.Value == bestCategoryVotes)
								{
									bestCategoryList.Add(category.Key);
								}
							}

							var bestCategory = bestCategoryList[Randomizer.Next(bestCategoryList.Count)];

							game.Value.AcknowledgedCategories.Add(bestCategory);
							game.Value.CurrentQuestions = await _quizService.GetRandomQuestionsFromCategoryAsync(bestCategory, game.Value.QuestionsPerCategoryCount, game.Value.AcknowledgedQuestions);
							game.Value.CurrentQuestionIndex = 0;
							game.Value.CurrentCategory = bestCategory;
						}
						else if (game.Value.GameMode == QuizGameMode.AllRandom)
						{
							game.Value.AcknowledgedCategories.Add(game.Value.AcknowledgedCategories.Count.ToString());
							game.Value.CurrentQuestions = await _quizService.GetRandomQuestionsFromCategoriesAsync(game.Value.ExcludedCategoriesList, game.Value.QuestionsPerCategoryCount, game.Value.AcknowledgedQuestions);
							game.Value.CurrentQuestionIndex = 0;
							game.Value.CurrentCategory = string.Empty;
						}

						game.Value.PrepareForQuestionsTime = currentTime - 2000;
						game.Value.GameState = QuizGameState.PrepareForQuestions;

						foreach (var question in game.Value.CurrentQuestions)
							game.Value.AcknowledgedQuestions.Add(question.Id);

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "category_vote_end",
							Value = new
							{
								Category = game.Value.CurrentCategory,
								PreloadImages = game.Value.CurrentQuestions.Select(x => x.Image).Where(x => !string.IsNullOrEmpty(x)),
								QuestionsCount = game.Value.CurrentQuestions.Count,
								QuestionIndex = game.Value.CurrentQuestionIndex
							}
						}, _jsonSerializerSettings)), game.Key);
					}
				}
				else if (game.Value.GameState == QuizGameState.PrepareForQuestions && currentTime - game.Value.PrepareForQuestionsTime > 2000)
				{
					foreach (var player in game.Value.CurrentPlayers)
						player.Value.AnswerId = string.Empty;

					var question = game.Value.CurrentQuestions[game.Value.CurrentQuestionIndex];

					game.Value.CurrentQuestionsAnswers = (await _quizService.GetAnswersAsync(question.Answers)).OrderBy(x => Randomizer.Next()).ToList();
					game.Value.QuestionAnsweringTime = currentTime;
					game.Value.GameState = QuizGameState.QuestionAnswering;

					await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
					{
						Type = "question_start",
						Value = new
						{
							Id = question.Id,
							Text = question.Text,
							Image = question.Image,
							QuestionStartTime = game.Value.QuestionAnsweringTime,
							Answers = game.Value.CurrentQuestionsAnswers.Select(x => new { Id = x.Id, Text = x.Text })
						}
					}, _jsonSerializerSettings)), game.Key);
				}
				else if (game.Value.GameState == QuizGameState.QuestionAnswering)
				{
					if (currentTime - game.Value.QuestionAnsweringTime > (game.Value.TimeSecondForQuestion * 1000 + 1500)
					   || !game.Value.CurrentPlayers.Values.Any(x => string.IsNullOrEmpty(x.AnswerId)))
					{
						game.Value.PrepareForQuestionsTime = currentTime;
						game.Value.GameState = QuizGameState.QuestionAnswered;

						HashSet<string> playerLeft = new HashSet<string>();
						List<Tuple<string, string>> playersAnswers = new List<Tuple<string, string>>();

						var currentQuestion = game.Value.CurrentQuestions[game.Value.CurrentQuestionIndex];

						foreach (var player in game.Value.CurrentPlayers)
						{
							player.Value.AnswersStatus.Add(player.Value.AnswerId == currentQuestion.CorrectAnswer);

							if (string.IsNullOrEmpty(player.Value.AnswerId))
							{
								playerLeft.Add(player.Key);
								continue;
							}

							playersAnswers.Add(new Tuple<string, string>(player.Key, player.Value.AnswerId));
						}

						if (playerLeft.Count > 0)
						{
							await _webSocketConnectionManager.NotifyUsers(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							{
								Type = "question_answer",
								Value = new
								{
									QuestionId = currentQuestion.Id,
									AnswerId = (string)null,
									CorrectAnswer = currentQuestion.CorrectAnswer,
									SkippedPlayers = playerLeft,
									OtherPlayers = playersAnswers.Select(x => new { PlayerName = x.Item1, AnswerId = x.Item2 })
								}
							}, _jsonSerializerSettings)), game.Key, playerLeft);

							if (playersAnswers.Count > 0)
							{
								await _webSocketConnectionManager.NotifyUsers(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								{
									Type = "question_timeout_players",
									Value = new
									{
										QuestionId = currentQuestion.Id,
										SkippedPlayers = playerLeft
									}
								}, _jsonSerializerSettings)), game.Key, playersAnswers.Select(x => x.Item1));
							}
						}
					}
				}
				else if (game.Value.GameState == QuizGameState.QuestionAnswered && currentTime - game.Value.PrepareForQuestionsTime >= 1000)
				{
					game.Value.CurrentQuestionIndex++;
					if (game.Value.CurrentQuestionIndex < game.Value.CurrentQuestions.Count)
					{
						game.Value.PrepareForQuestionsTime = currentTime;
						game.Value.GameState = QuizGameState.PrepareForQuestions;

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "preare_next_question",
							Value = new
							{
								QuestionIndex = game.Value.CurrentQuestionIndex
							}
						}, _jsonSerializerSettings)), game.Key);
					}
					else
					{
						game.Value.GameState = QuizGameState.RoundEnd;
						game.Value.PrepareForQuestionsTime = currentTime;

						var questionsCount = game.Value.CurrentQuestionIndex;
						var maxPoints = game.Value.CurrentPlayers.Count * 100;

						if (questionsCount > 0)
						{
							foreach (var player in game.Value.CurrentPlayers)
							{
								var playerPoints = player.Value.AnswersStatus.Sum(x => x ? 1 : 0);
								var ratio = (double)playerPoints / questionsCount;

								player.Value.Points += (ratio * maxPoints);
							}
						}

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "round_end",
							Value = (string)null
						}, _jsonSerializerSettings)), game.Key);
					}
				}
				else if (game.Value.GameState == QuizGameState.RoundEnd && currentTime - game.Value.PrepareForQuestionsTime > 2000)
				{
					if (game.Value.AcknowledgedCategories.Count >= game.Value.MaxCategoriesCount)
					{
						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "game_finished",
							Value = new
							{
								PlayersRankings = game.Value.CurrentPlayers.Select(x => new { PlayerName = x.Key, Points = x.Value.Points })
							}
						}, _jsonSerializerSettings)), game.Key);

						game.Value.AcknowledgedCategories.Clear();
						game.Value.AcknowledgedQuestions.Clear();
						game.Value.CurrentVoteCategories.Clear();
						game.Value.CurrentQuestionsAnswers.Clear();

						foreach (var player in game.Value.CurrentPlayers)
						{
							player.Value.IsReady = false;
							player.Value.AnswersStatus.Clear();
							player.Value.Points = 0;
							player.Value.AnswerId = string.Empty;
							player.Value.CategoryVoteId = string.Empty;
						}

						game.Value.CreateTime = currentTime;
						game.Value.GameState = QuizGameState.Lobby;
					}
					else
						await PrepareCategoryVote(game.Key);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception while updating game " + e);
			}
		}

		public async Task Tick()
		{
			try
			{
				ConcurrentBag<string> gamesEnded = new ConcurrentBag<string>();
				List<Task> gamesTasks = new List<Task>();

				foreach (var game in _games)
				{
					gamesTasks.Add(Task.Run(() => GameUpdateTick(game, gamesEnded)));
				}

				await Task.WhenAll(gamesTasks);

				foreach (var gameId in gamesEnded)
				{
					RemoveGame(gameId);
					await _webSocketConnectionManager.RemoveGameSockets(gameId);
				}
			}
			catch (Exception e) { Console.WriteLine("Tick update failed " + e); }
		}
	}
}
