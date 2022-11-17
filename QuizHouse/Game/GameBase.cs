using Newtonsoft.Json;
using QuizHouse.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuizHouse.Game
{
	public class GameBase : GameDTO
	{
		public enum GameState
		{
			None,
			Lobby,
			CategorySelection,
			PrepareForQuestions,
			QuestionAnswering,
			QuestionAnswered,
			RoundEnd
		};

		public ConcurrentDictionary<string, GamePlayerBase> PlayersDict = new ConcurrentDictionary<string, GamePlayerBase>();
		public List<QuestionDTO> QuestionsData = new List<QuestionDTO>();
		public List<AnswerDTO> QuestionsAnswerData = new List<AnswerDTO>();
		public int CurrentQuestionIndex { get; set; }
		public GameState CurrentGameState { get; set; }
		public long LastTakenAction { get; set; }

		public bool PlayerInGame(string accountId)
		{
			return PlayersDict.ContainsKey(accountId);
		}

		public bool TryAssignSocketToPlayer(string accountId, WebSocket socket)
		{
			if (!PlayersDict.TryGetValue(accountId, out var playerBase))
				return false;

			playerBase.AssignedSocket = socket;

			return true;
		}

		public bool InvalidateSocket(string accountId, WebSocketState state)
		{
			if (!PlayersDict.TryGetValue(accountId, out var playerBase))
				return false;

			playerBase.AssignedSocket = null;
			playerBase.TimeoutTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			playerBase.TimeoutTime += state == WebSocketState.CloseReceived ? GameConstSettings.TIMEOUT_LOBBY_CLOSE : GameConstSettings.TIMEOUT_LOBBY_UNKNOWN;

			return true;
		}

		public byte[] GetGameStatePacket(string accountId)
		{
			if (!PlayersDict.TryGetValue((accountId), out var playerBase))
				return null;

			if (CurrentGameState == GameState.PrepareForQuestions)
			{
				var playerAlreadyAnswered = (CurrentGameState == GameState.QuestionAnswered || !string.IsNullOrEmpty(playerBase.AnswerId));
				var currentQuestion = QuestionsData[CurrentQuestionIndex < QuestionsData.Count ? CurrentQuestionIndex : (QuestionsData.Count - 1)];

				return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
				{
					Type = "game_restore",
					Value = new
					{
						ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
						Players = PlayersDict.Values.Select(x => new
						{
							x.Id,
							x.Username,
							x.AnswersData,
							AnswerId = playerAlreadyAnswered ? x.AnswerId : string.Empty
						}),
						PlayerId = playerBase.Id,
						GameState = CurrentGameState.ToString(),

						CorrentAnswerId = playerAlreadyAnswered ? currentQuestion.CorrectAnswer : "",
						QuestionAnswerId = playerBase.AnswerId,
						CategoryId = Categories[Categories.Count - 1],
						PreloadImages = QuestionsData.Where(x => string.IsNullOrEmpty(x.Image) == false).Select(x => x.Image),
						QuestionsCount = QuestionsData.Count,
						QuestionIndex = CurrentQuestionIndex,
						QuestionData = new
						{
							currentQuestion.Id,
							currentQuestion.Text,
							currentQuestion.Image,
							QuestionStartTime = LastTakenAction,
							Answers = QuestionsAnswerData
						}
					}
				}));
			}

			return null;
		}

		public async Task NotifyAll(string data, IEnumerable<string> skip)
		{
			byte[] packetData = Encoding.UTF8.GetBytes(data);

			foreach (var player in PlayersDict.Values)
			{
				if (player.AssignedSocket == null)
					continue;

				if (player.AssignedSocket.State != WebSocketState.Open)
					continue;

				if (skip.Contains(player.Id))
					continue;

				await player.AssignedSocket.SendAsync(
										new ArraySegment<byte>(packetData),
										WebSocketMessageType.Text,
										true,
										CancellationToken.None);
			}
		}

		public async Task AbortGame()
		{
			GameStatus = GameStatusDTO.Aborted;

			foreach (var player in PlayersDict.Values)
			{
				if (player.AssignedSocket == null)
					continue;

				if (player.AssignedSocket.State != WebSocketState.Open &&
					player.AssignedSocket.State != WebSocketState.CloseReceived)
					continue;

				await player.AssignedSocket.CloseAsync((WebSocketCloseStatus)3002, "Game aborted", CancellationToken.None);
			}
		}

		public void AddPlayer(GamePlayerBase gamePlayer)
		{
			Players.Add(gamePlayer);
			PlayersDict.TryAdd(gamePlayer.Id, gamePlayer);
		}

		public async Task ChangeToQuestionsStage(string categoryId, List<QuestionDTO> questions)
		{
			foreach (var player in PlayersDict.Values)
			{
				player.Categories.Add(string.IsNullOrEmpty(player.CategoryVoteId) ? "000000000000000000000000" : player.CategoryVoteId);
				player.CategoryVoteId = string.Empty;
				player.AnswersData.Clear();
			}

			Categories.Add(categoryId);
			Questions.AddRange(questions.Select(x => x.Id));

			QuestionsData = questions;
			CurrentQuestionIndex = 0;

			CurrentGameState = GameState.PrepareForQuestions;
			LastTakenAction = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			var questionsCount = questions.Count;
			var preloadImages = QuestionsData.Where(x => string.IsNullOrEmpty(x.Image) == false).Select(x => x.Image);

			await NotifyAll(JsonConvert.SerializeObject(new
			{
				Type = "category_vote_end",
				Value = new
				{
					categoryId,
					preloadImages,
					questionsCount,
					QuestionIndex = 0
				}
			}), Enumerable.Empty<string>());
		}

		public async Task<bool> Tick()
		{
			if (CurrentGameState == GameState.None)
				return true;

			bool abortGame = PlayersDict.IsEmpty;
			if (!abortGame
				&& CurrentGameState != GameState.QuestionAnswering
				&& CurrentGameState != GameState.CategorySelection)
			{
				var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (PlayersDict.Count == PlayersDict.Values.Where(x => x.AssignedSocket == null && currentTime >= x.TimeoutTime).Count())
					abortGame = true;
			}

			if (abortGame)
				return false;

			switch (CurrentGameState)
			{
				case GameState.Lobby:
					await LobbyStageTick();
					break;
				case GameState.PrepareForQuestions:
					await PrepareForQuestionsStageTick();
					break;
			}

			return true;
		}

		private async Task LobbyStageTick()
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			var timeoutPlayers = PlayersDict.Values.Where(x => x.AssignedSocket == null && currentTime >= x.TimeoutTime).Select(x => x.Id).ToArray();
			foreach (var player in timeoutPlayers)
				PlayersDict.TryRemove(player, out _);

			if (timeoutPlayers.Length > 0)
			{
				await NotifyAll(JsonConvert.SerializeObject(new
				{
					Type = "timeout_players",
					Value = new
					{
						timeoutPlayers
					}
				}), Enumerable.Empty<string>());
			}
		}

		private async Task PrepareForQuestionsStageTick()
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - LastTakenAction < GameConstSettings.PREPARE_FOR_QUESTION_TIME)
				return;


		}
	}
}
