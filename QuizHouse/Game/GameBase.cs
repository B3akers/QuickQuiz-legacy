using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuizHouse.Dto;
using QuizHouse.Services;
using QuizHouse.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
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
			CategorySelection,
			PrepareForQuestions,
			QuestionAnswering,
			QuestionAnswered,
			RoundEnd
		};

		public ConcurrentDictionary<string, GamePlayerBase> PlayersDict = new ConcurrentDictionary<string, GamePlayerBase>();
		public List<QuestionDTO> QuestionsData = new List<QuestionDTO>();
		public List<AnswerDTO> QuestionsAnswerData = new List<AnswerDTO>();
		public GameSettings GameSettings = new GameSettings();
		public int CurrentQuestionIndex { get; set; }
		public GameState CurrentGameState { get; set; }
		public long LastTakenAction { get; set; }

		public bool PlayerInGame(string accountId)
		{
			return PlayersDict.ContainsKey(accountId);
		}

		public GamePlayerBase GetPlayer(string accountId)
		{
			if (PlayersDict.TryGetValue(accountId, out var playerBase))
				return playerBase;
			return null;
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

			if (CurrentGameState != GameState.CategorySelection)
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
							x.CustomColor,
							x.ConnectionsInfo,
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
							GameSettings.QuestionAnswerTime,
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

		public async Task NotifyUsers(string data, IEnumerable<string> users)
		{
			byte[] packetData = Encoding.UTF8.GetBytes(data);

			foreach (var playerId in users)
			{
				if (!PlayersDict.TryGetValue(playerId, out var player))
					continue;

				if (player.AssignedSocket == null)
					continue;

				if (player.AssignedSocket.State != WebSocketState.Open)
					continue;

				await player.AssignedSocket.SendAsync(
										new ArraySegment<byte>(packetData),
										WebSocketMessageType.Text,
										true,
										CancellationToken.None);
			}
		}

		public async Task HandlePlayerData(GamePlayerBase playerBase, string data)
		{
			var packet = JObject.Parse(data);
			var type = packet["type"].ToString();
			if (type == "question_answer")
			{
				if (CurrentGameState != GameState.QuestionAnswering) return;

				if (!string.IsNullOrEmpty(playerBase.AnswerId))
					return;

				if (CurrentQuestionIndex >= QuestionsData.Count) return;

				var questionId = packet["questionId"].ToString();
				var answerId = packet["answerId"].ToString();

				var currentQuestion = QuestionsData[CurrentQuestionIndex];
				if (currentQuestion.Id != questionId)
					return;

				HashSet<string> notifyPlayers = new HashSet<string>();
				List<Tuple<string, string>> playersAnswers = new List<Tuple<string, string>>();

				foreach (var player in PlayersDict.Values)
				{
					if (!string.IsNullOrEmpty(player.AnswerId))
					{
						notifyPlayers.Add(player.Id);
						playersAnswers.Add(new Tuple<string, string>(player.Id, player.AnswerId));
					}
				}

				if (notifyPlayers.Count > 0)
				{
					await NotifyUsers(JsonConvert.SerializeObject(new
					{
						Type = "player_answer_question",
						Value = new
						{
							PlayerId = playerBase.Id,
							QuestionId = currentQuestion.Id,
							answerId,
							IsCorrect = answerId == currentQuestion.CorrectAnswer
						}
					}), notifyPlayers);
				}

				playerBase.AnswerId = answerId;
				playerBase.AnswerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastTakenAction;

				await playerBase.AssignedSocket.SendAsync(
					new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
					{
						Type = "question_answer",
						Value = new
						{
							QuestionId = currentQuestion.Id,
							answerId,
							currentQuestion.CorrectAnswer,
							OtherPlayers = playersAnswers
						}
					}))),
					WebSocketMessageType.Text,
					true,
					CancellationToken.None);
			}
		}

		public async Task FinishGame(bool isAborted)
		{
			GameStatus = isAborted ? GameStatusDTO.Aborted : GameStatusDTO.Finished;

			foreach (var player in PlayersDict.Values)
			{
				if (player.AssignedSocket == null)
					continue;

				if (player.AssignedSocket.State != WebSocketState.Open &&
					player.AssignedSocket.State != WebSocketState.CloseReceived)
					continue;

				await player.AssignedSocket.CloseAsync(isAborted ? (WebSocketCloseStatus)3002 : (WebSocketCloseStatus)3004, isAborted ? "Game aborted" : "Game finished", CancellationToken.None);
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

		public async Task<bool> Tick(DatabaseService databaseService)
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
				case GameState.PrepareForQuestions:
					await PrepareForQuestionsStageTick(databaseService);
					break;
				case GameState.QuestionAnswering:
					await QuestionAnsweringStageTick();
					break;
				case GameState.QuestionAnswered:
					await QuestionAnsweredStageTick();
					break;
				case GameState.RoundEnd:
					return await RoundEndStageTick();
			}

			return true;
		}

		private async Task<bool> RoundEndStageTick()
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - LastTakenAction < GameConstSettings.ROUND_END_TIME)
				return true;

			if (GameType == GameTypeDTO.Solo)
			{
				GameStatus = GameStatusDTO.Finished;

				await NotifyAll(JsonConvert.SerializeObject(new
				{
					Type = "game_finished",
					Value = new
					{
						PlayersRankings = PlayersDict.Values.Select(x => new { PlayerId = x.Id, x.Points })
					}
				}), Enumerable.Empty<string>());

				return false;
			}

			return true;
		}

		private async Task QuestionAnsweredStageTick()
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - LastTakenAction < GameConstSettings.QUESTION_ANSWERED_TIME)
				return;

			LastTakenAction = currentTime;

			CurrentQuestionIndex++;
			if (CurrentQuestionIndex < QuestionsData.Count)
			{
				CurrentGameState = GameState.PrepareForQuestions;

				await NotifyAll(JsonConvert.SerializeObject(new
				{
					Type = "preare_next_question",
					Value = new
					{
						QuestionIndex = CurrentQuestionIndex
					}
				}), Enumerable.Empty<string>());
			}
			else
			{
				CurrentGameState = GameState.RoundEnd;

				var questionsCount = CurrentQuestionIndex;
				if (questionsCount > 0)
				{
					long[] questionsAnswerTimes = new long[questionsCount];
					int[] questionsAnswersCount = new int[questionsCount];

					foreach (var player in PlayersDict.Values)
					{
						for (var i = 0; i < player.AnswersData.Count; i++)
						{
							var answer = player.AnswersData[i];

							if (!answer.Item1)
								continue;

							questionsAnswerTimes[i] += answer.Item2;
							questionsAnswersCount[i]++;
						}
					}

					double[] questionsAnswerTimesAvg = new double[questionsCount];
					for (var i = 0; i < questionsCount; i++)
					{
						if (questionsAnswersCount[i] == 0)
							continue;

						questionsAnswerTimesAvg[i] = (double)questionsAnswerTimes[i] / questionsAnswersCount[i];
					}

					foreach (var player in PlayersDict.Values)
					{
						for (var i = 0; i < player.AnswersData.Count; i++)
						{
							var answer = player.AnswersData[i];
							if (!answer.Item1)
								continue;

							player.Points += 100.0 * (questionsAnswerTimesAvg[i] / answer.Item2);
						}
					}
				}

				await NotifyAll(JsonConvert.SerializeObject(new
				{
					Type = "round_end",
					Value = (string)null
				}), Enumerable.Empty<string>());
			}

		}

		private async Task QuestionAnsweringStageTick()
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - LastTakenAction < (GameSettings.QuestionAnswerTime * 1000) && PlayersDict.Values.Any(x => string.IsNullOrEmpty(x.AnswerId)))
				return;

			CurrentGameState = GameState.QuestionAnswered;
			LastTakenAction = currentTime;

			HashSet<string> playerLeft = new HashSet<string>();
			List<Tuple<string, string>> playersAnswers = new List<Tuple<string, string>>();

			var currentQuestion = QuestionsData[CurrentQuestionIndex];

			foreach (var player in PlayersDict.Values)
			{
				if (string.IsNullOrEmpty(player.AnswerId))
				{
					player.AnswersData.Add(new Tuple<bool, long>(false, 0));
					player.Answers.Add(new GamePlayerSelectDTO() { Id = "000000000000000000000000", Time = 0 });
					playerLeft.Add(player.Id);
				}
				else
				{
					player.AnswersData.Add(new Tuple<bool, long>(player.AnswerId == currentQuestion.CorrectAnswer, player.AnswerTime));
					player.Answers.Add(new GamePlayerSelectDTO() { Id = player.AnswerId, Time = player.AnswerTime });
					playersAnswers.Add(new Tuple<string, string>(player.Id, player.AnswerId));
				}
			}

			if (playerLeft.Count > 0)
			{
				await NotifyUsers(JsonConvert.SerializeObject(new
				{
					Type = "question_answer",
					Value = new
					{
						QuestionId = currentQuestion.Id,
						AnswerId = (string)null,
						currentQuestion.CorrectAnswer,
						SkippedPlayers = playerLeft,
						OtherPlayers = playersAnswers
					}
				}), playerLeft);

				if (playersAnswers.Count > 0)
				{
					await NotifyUsers(JsonConvert.SerializeObject(new
					{
						Type = "question_timeout_players",
						Value = new
						{
							QuestionId = currentQuestion.Id,
							SkippedPlayers = playerLeft
						}
					}), playersAnswers.Select(x => x.Item1));
				}
			}
		}

		private async Task PrepareForQuestionsStageTick(DatabaseService databaseService)
		{
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - LastTakenAction < GameConstSettings.PREPARE_FOR_QUESTION_TIME)
				return;

			foreach (var player in PlayersDict.Values)
				player.AnswerId = string.Empty;

			var question = QuestionsData[CurrentQuestionIndex];

			QuestionsAnswerData = (await databaseService.GetAnswersAsync(question.Answers)).OrderBy(x => Randomizer.Next()).ToList();
			LastTakenAction = currentTime;
			CurrentGameState = GameState.QuestionAnswering;

			await NotifyAll(JsonConvert.SerializeObject(new
			{
				Type = "question_start",
				Value = new
				{
					question.Id,
					question.Text,
					question.Image,
					QuestionStartTime = LastTakenAction,
					GameSettings.QuestionAnswerTime,
					Answers = QuestionsAnswerData
				}
			}), Enumerable.Empty<string>());
		}
	}
}
