using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using QuizHouse.Interfaces;
using QuizHouse.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuizHouse.WebSockets
{
	public class WebSocketPacket
	{
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("value")]
		public string Value { get; set; }
	}

	public class LoginWebSocketPacket
	{
		[JsonProperty("token")]
		public string Token { get; set; }
	}

	public class LobbyReadySocketPacket
	{
		[JsonProperty("ready")]
		public bool IsReady { get; set; }
	}

	public class CategoryVoteSocketPacket
	{
		[JsonProperty("categoryId")]
		public string CategoryId { get; set; }
	}

	public class PlayerKickSocketPacket
	{
		[JsonProperty("playerName")]
		public string PlayerName { get; set; }
	}

	public class LobbyChangePacketPacket
	{
		[JsonProperty("lobbyMode")]
		public QuizLobbyMode LobbyMode { get; set; }
	}

	public class QuestionAnswerSocketPacket
	{
		[JsonProperty("answerId")]
		public string AnswerId { get; set; }

		[JsonProperty("questionId")]
		public string QuestionId { get; set; }
	}

	public class StartGameSocketPacket
	{
		[JsonProperty("settingsRoundCount")]
		public int SettingsRoundCount { get; set; }

		[JsonProperty("questionsPerCategoryCount")]
		public int QuestionsPerCategoryCount { get; set; }

		[JsonProperty("settingsTimeForQuestion")]
		public int SettingsTimeForQuestion { get; set; }

		[JsonProperty("settingsCategoryPerSelection")]
		public int SettingsCategoryPerSelection { get; set; }

		[JsonProperty("settingsGameMode")]
		public int SettingsGameMode { get; set; }

		[JsonProperty("excludedCategoriesList")]
		public List<string> ExcludedCategoriesList { get; set; }
	}

	public class WebsocketPlayerConnection
	{
		public WebSocket Socket { get; set; }
		public string Username { get; set; }
		public string GameId { get; set; }
		public bool SocketAuthorized { get; set; }
		public long LastPingTime { get; set; }
	}
	public class ConnectionManagerOld
	{
		private ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _gameSockets = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();

		public bool RemoveSocket(WebSocket socket, string gameId, string userName)
		{
			if (_gameSockets.TryGetValue(gameId, out var playersSockets))
				if (playersSockets.TryGetValue(userName, out var playerSocket))
					if (playerSocket == socket)
						return playersSockets.TryRemove(userName, out var deletedSocket);

			return false;
		}

		public async Task KickPlayer(string gameId, string userName)
		{
			if (_gameSockets.TryGetValue(gameId, out var playersSockets))
				if (playersSockets.TryGetValue(userName, out var playerSocket))
					if (playerSocket.State == WebSocketState.Open)
						await playerSocket.CloseOutputAsync((WebSocketCloseStatus)3300,
							   "Kicked",
							   CancellationToken.None);

		}

		public async Task<bool> RemoveGameSockets(string gameId)
		{
			var result = _gameSockets.TryRemove(gameId, out var old);

			if (result)
				foreach (var client in old)
					if (client.Value.State == WebSocketState.Open)
						await client.Value.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
								string.Empty,
								CancellationToken.None);

			return result;
		}

		public bool AddSocket(WebSocket socket, string gameId, string userName)
		{
			bool socketReplaced = false;

			var playersSockets = _gameSockets.GetOrAdd(gameId, x =>
			{
				return new ConcurrentDictionary<string, WebSocket>();
			});

			playersSockets.AddOrUpdate(userName, socket, (key, oldValue) =>
			{
				_ = oldValue.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
							   string.Empty,
							   CancellationToken.None);

				socketReplaced = true;

				return socket;
			});

			return socketReplaced;
		}

		public async Task NotifyAll(byte[] packetData, string gameId, string skipUserName = null)
		{
			if (_gameSockets.TryGetValue(gameId, out var playersSockets))
			{
				foreach (var playerSocket in playersSockets)
				{
					if (!string.IsNullOrEmpty(skipUserName) && playerSocket.Key == skipUserName)
						continue;

					if (playerSocket.Value.State != WebSocketState.Open)
						continue;

					await playerSocket.Value.SendAsync(
											new ArraySegment<byte>(packetData),
											WebSocketMessageType.Text,
											true,
											CancellationToken.None);
				}
			}
		}

		public async Task NotifyUsers(byte[] packetData, string gameId, IEnumerable<string> users)
		{
			if (_gameSockets.TryGetValue(gameId, out var playersSockets))
			{
				foreach (var playerSocket in playersSockets)
				{
					if (!users.Contains(playerSocket.Key))
						continue;

					if (playerSocket.Value.State != WebSocketState.Open)
						continue;

					await playerSocket.Value.SendAsync(
											new ArraySegment<byte>(packetData),
											WebSocketMessageType.Text,
											true,
											CancellationToken.None);
				}
			}
		}
	}

	public class WebSocketHandlerOld
	{
		private ConnectionManagerOld _webSocketConnectionManager;
		private GamesServiceOld _gamesService;
		private IJwtTokenHandler _jwtTokenHandler;

		private JsonSerializerSettings _jsonSerializerSettings;

		public WebSocketHandlerOld(ConnectionManagerOld webSocketConnectionManager, GamesServiceOld gamesService, IJwtTokenHandler jwtTokenHandler)
		{
			_webSocketConnectionManager = webSocketConnectionManager;
			_gamesService = gamesService;
			_jwtTokenHandler = jwtTokenHandler;

			_jsonSerializerSettings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
		}

		private async Task HandlePacket(WebsocketPlayerConnection socketPlayerConnection, WebSocketPacket packet)
		{
			try
			{
				if (socketPlayerConnection.SocketAuthorized)
				{
					if (packet.Type == "lobby_ready")
					{
						var lobbyReady = JsonConvert.DeserializeObject<LobbyReadySocketPacket>(packet.Value);
						var game = _gamesService.FindGame(socketPlayerConnection.GameId);

						if (game == null
							|| game.GameState != QuizGameState.Lobby)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (quizPlayer.IsOwner)
							return;

						if (!game.CurrentPlayers.TryGetValue(game.OwnerName, out var onwerPlayer))
							return;

						if (!onwerPlayer.IsReady)
							return;

						quizPlayer.IsReady = lobbyReady.IsReady;

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "lobby_ready",
							Value = new
							{
								IsReady = lobbyReady.IsReady,
								PlayerName = socketPlayerConnection.Username
							}
						}, _jsonSerializerSettings)), socketPlayerConnection.GameId);
					}
					else if (packet.Type == "game_kick")
					{
						var kickPacket = JsonConvert.DeserializeObject<PlayerKickSocketPacket>(packet.Value);
						var game = _gamesService.FindGame(socketPlayerConnection.GameId);
						if (game == null)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!quizPlayer.IsOwner)
							return;

						if (!game.CurrentPlayers.TryGetValue(kickPacket.PlayerName, out var quizKickPlayer))
							return;

						quizKickPlayer.Disconnected = true;
						quizKickPlayer.LastPingTime = 0;

						await _webSocketConnectionManager.KickPlayer(socketPlayerConnection.GameId, kickPacket.PlayerName);
					}
					else if (packet.Type == "question_answer")
					{
						var answer = JsonConvert.DeserializeObject<QuestionAnswerSocketPacket>(packet.Value);
						if (string.IsNullOrEmpty(answer.AnswerId) || string.IsNullOrEmpty(answer.QuestionId))
							return;

						var game = _gamesService.FindGame(socketPlayerConnection.GameId);

						if (game == null
							|| game.GameState != QuizGameState.QuestionAnswering)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!string.IsNullOrEmpty(quizPlayer.AnswerId))
							return;

						var currentQuestion = game.CurrentQuestions[game.CurrentQuestionIndex];

						if (currentQuestion.Id != answer.QuestionId)
							return;

						HashSet<string> notifyPlayers = new HashSet<string>();
						List<Tuple<string, string>> playersAnswers = new List<Tuple<string, string>>();

						foreach (var player in game.CurrentPlayers)
						{
							if (!string.IsNullOrEmpty(player.Value.AnswerId))
							{
								notifyPlayers.Add(player.Key);
								playersAnswers.Add(new Tuple<string, string>(player.Key, player.Value.AnswerId));
							}
						}

						if (notifyPlayers.Count > 0)
						{
							await _webSocketConnectionManager.NotifyUsers(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							{
								Type = "player_answer_question",
								Value = new
								{
									PlayerName = socketPlayerConnection.Username,
									QuestionId = currentQuestion.Id,
									AnswerId = answer.AnswerId,
									IsCorrect = answer.AnswerId == currentQuestion.CorrectAnswer
								}
							}, _jsonSerializerSettings)), socketPlayerConnection.GameId, notifyPlayers);
						}

						quizPlayer.AnswerId = answer.AnswerId;

						await socketPlayerConnection.Socket.SendAsync(
							new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							{
								Type = "question_answer",
								Value = new
								{
									QuestionId = currentQuestion.Id,
									AnswerId = answer.AnswerId,
									CorrectAnswer = currentQuestion.CorrectAnswer,
									OtherPlayers = playersAnswers.Select(x => new { PlayerName = x.Item1, AnswerId = x.Item2 }).ToArray()
								}
							}, _jsonSerializerSettings))),
							WebSocketMessageType.Text,
							true,
							CancellationToken.None);
					}
					else if (packet.Type == "category_vote")
					{
						var vote = JsonConvert.DeserializeObject<CategoryVoteSocketPacket>(packet.Value);

						if (string.IsNullOrEmpty(vote.CategoryId))
							return;

						var game = _gamesService.FindGame(socketPlayerConnection.GameId);

						if (game == null
							|| game.GameState != QuizGameState.CategorySelection)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!string.IsNullOrEmpty(quizPlayer.CategoryVoteId))
							return;

						if (game.CurrentVoteCategories.TryGetValue(vote.CategoryId, out var currentCount))
						{
							game.CurrentVoteCategories[vote.CategoryId] = currentCount + 1;
							quizPlayer.CategoryVoteId = vote.CategoryId;

							await socketPlayerConnection.Socket.SendAsync(
							 new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							 {
								 Type = "category_vote",
								 Value = new
								 {
									 CategoryId = vote.CategoryId
								 }
							 }, _jsonSerializerSettings))),
							 WebSocketMessageType.Text,
							 true,
							 CancellationToken.None);
						}
					}
					else if (packet.Type == "transfer_owner")
					{
						var game = _gamesService.FindGame(socketPlayerConnection.GameId);
						if (game == null)
							return;

						var owenrPacket = JsonConvert.DeserializeObject<PlayerKickSocketPacket>(packet.Value);

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!quizPlayer.IsOwner)
							return;

						if (!game.CurrentPlayers.TryGetValue(owenrPacket.PlayerName, out var newOwner))
							return;

						quizPlayer.IsOwner = false;
						newOwner.IsOwner = true;
						game.OwnerName = owenrPacket.PlayerName;

						foreach (var player in game.CurrentPlayers)
							player.Value.IsReady = false;

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "owner_transfer",
							Value = new
							{
								PlayerName = owenrPacket.PlayerName
							}
						}, _jsonSerializerSettings)), socketPlayerConnection.GameId);
					}
					else if (packet.Type == "change_lobby_mode")
					{
						var game = _gamesService.FindGame(socketPlayerConnection.GameId);
						if (game == null)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!quizPlayer.IsOwner)
							return;

						var lobbyChangePacket = JsonConvert.DeserializeObject<LobbyChangePacketPacket>(packet.Value);

						game.LobbyMode = lobbyChangePacket.LobbyMode;
						if (game.LobbyMode > QuizLobbyMode.Max)
							game.LobbyMode = QuizLobbyMode.Max - 1;
						if (game.LobbyMode < 0)
							game.LobbyMode = QuizLobbyMode.Normal;

						await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
						{
							Type = "lobby_mode_changed",
							Value = new
							{
								LobbyMode = game.LobbyMode
							}
						}, _jsonSerializerSettings)), socketPlayerConnection.GameId);
					}
					else if (packet.Type == "start_game")
					{
						var startGame = JsonConvert.DeserializeObject<StartGameSocketPacket>(packet.Value);

						var game = _gamesService.FindGame(socketPlayerConnection.GameId);
						if (game == null
							|| game.GameState != QuizGameState.Lobby)
							return;

						if (game.CurrentPlayers.Count < 1)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						if (!quizPlayer.IsOwner)
							return;

						quizPlayer.IsReady = !quizPlayer.IsReady;

						if (!quizPlayer.IsReady)
						{
							foreach (var player in game.CurrentPlayers)
								player.Value.IsReady = false;

							await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							{
								Type = "lobby_ready",
								Value = new
								{
									IsReady = quizPlayer.IsReady,
									PlayerName = socketPlayerConnection.Username
								}
							}, _jsonSerializerSettings)), socketPlayerConnection.GameId);
						}
						else
						{
							var settingsRoundCount = Math.Clamp(startGame.SettingsRoundCount, 1, 50);
							var questionsPerCategoryCount = Math.Clamp(startGame.QuestionsPerCategoryCount, 1, 15);
							var settingsTimeForQuestion = Math.Clamp(startGame.SettingsTimeForQuestion, 1, 45);
							var settingsCategoryPerSelection = Math.Clamp(startGame.SettingsCategoryPerSelection, 1, 25);
							var settingsGameMode = Math.Clamp(startGame.SettingsGameMode, 0, 1);

							startGame.ExcludedCategoriesList.RemoveAll(x => !ObjectId.TryParse(x, out _));

							game.MaxCategoriesCount = settingsRoundCount;
							game.QuestionsPerCategoryCount = questionsPerCategoryCount;
							game.TimeSecondForQuestion = settingsTimeForQuestion;
							game.CategoryPerSelectionCount = settingsCategoryPerSelection;
							game.ExcludedCategoriesList = startGame.ExcludedCategoriesList;
							game.GameMode = (QuizGameMode)settingsGameMode;

							await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
							{
								Type = "lobby_ready",
								Value = new
								{
									IsReady = quizPlayer.IsReady,
									PlayerName = socketPlayerConnection.Username,
									Settings = new
									{
										RoundCount = game.MaxCategoriesCount,
										QuestionsPerCategory = game.QuestionsPerCategoryCount,
										TimeSecondForQuestion = game.TimeSecondForQuestion,
										CategoryPerSelectionCount = game.CategoryPerSelectionCount,
										GameMode = (int)game.GameMode,
										ExcludedCategoriesList = game.ExcludedCategoriesList
									}
								}
							}, _jsonSerializerSettings)), socketPlayerConnection.GameId);
						}
					}
				}
				else
				{
					if (packet.Type == "login" || packet.Type == "reconnect")
					{
						var token = JsonConvert.DeserializeObject<LoginWebSocketPacket>(packet.Value);

						if (!_jwtTokenHandler.ValidateToken(token.Token))
							return;

						var tokenHandler = new JwtSecurityTokenHandler();
						var securityToken = tokenHandler.ReadToken(token.Token) as JwtSecurityToken;

						var userNameClaim = securityToken.Claims.FirstOrDefault(x => x.Type == "username");
						var gameIdClaim = securityToken.Claims.FirstOrDefault(x => x.Type == "game_id");

						if (userNameClaim == null || gameIdClaim == null)
							return;

						socketPlayerConnection.Username = userNameClaim.Value;
						socketPlayerConnection.GameId = gameIdClaim.Value;

						var game = _gamesService.FindGame(socketPlayerConnection.GameId);
						if (game == null)
							return;

						if (!game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var quizPlayer))
							return;

						var sockedReplaced = _webSocketConnectionManager.AddSocket(socketPlayerConnection.Socket, socketPlayerConnection.GameId, socketPlayerConnection.Username);

						if (game.GameState == QuizGameState.Lobby)
						{
							await socketPlayerConnection.Socket.SendAsync(
								 new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								 {
									 Type = "lobby",
									 Value = new
									 {
										 ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
										 InviteCode = socketPlayerConnection.GameId,
										 OwnerName = game.OwnerName,
										 PlayerName = socketPlayerConnection.Username,
										 LobbyMode = game.LobbyMode,
										 Settings = new
										 {
											 RoundCount = game.MaxCategoriesCount,
											 QuestionsPerCategory = game.QuestionsPerCategoryCount,
											 TimeSecondForQuestion = game.TimeSecondForQuestion,
											 CategoryPerSelectionCount = game.CategoryPerSelectionCount,
											 GameMode = (int)game.GameMode,
											 ExcludedCategoriesList = game.ExcludedCategoriesList
										 },
										 LobbyPlayers = game.CurrentPlayers.Select(x => new
										 {
											 PlayerName = x.Key,
											 IsOwner = x.Value.IsOwner,
											 IsReady = x.Value.IsReady
										 })
									 }
								 }, _jsonSerializerSettings))),
								 WebSocketMessageType.Text,
								 true,
								 CancellationToken.None);

							if (packet.Type == "login" && !sockedReplaced)
							{
								await _webSocketConnectionManager.NotifyAll(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								{
									Type = "lobby_join",
									Value = new
									{
										IsOwner = game.OwnerName == socketPlayerConnection.Username,
										IsReady = false,
										PlayerName = socketPlayerConnection.Username
									}
								}, _jsonSerializerSettings)), socketPlayerConnection.GameId, socketPlayerConnection.Username);
							}
						}
						else if (packet.Type == "reconnect")
						{
							if (game.GameState == QuizGameState.CategorySelection)
							{
								await socketPlayerConnection.Socket.SendAsync(
								new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								{
									Type = "reconnect",
									Value = new
									{
										ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
										InviteCode = socketPlayerConnection.GameId,
										GameState = "CategorySelection",
										PlayerName = socketPlayerConnection.Username,
										LobbyMode = game.LobbyMode,
										LobbyPlayers = game.CurrentPlayers.Select(x => new
										{
											PlayerName = x.Key,
											IsOwner = x.Value.IsOwner,
											IsReady = x.Value.IsReady
										}),
										Settings = new
										{
											RoundCount = game.MaxCategoriesCount,
											QuestionsPerCategory = game.QuestionsPerCategoryCount,
											TimeSecondForQuestion = game.TimeSecondForQuestion,
											CategoryPerSelectionCount = game.CategoryPerSelectionCount,
											GameMode = (int)game.GameMode,
											ExcludedCategoriesList = game.ExcludedCategoriesList
										},
										CategoryStartTime = game.CategoryVoteTime,
										Categories = game.CurrentVoteCategories.Keys,
										CurrentCategoryIndex = game.AcknowledgedCategories.Count,
										MaxCategoryIndex = game.MaxCategoriesCount,
										CurrentCategoryVote = quizPlayer.CategoryVoteId,
										PlayersRankings = game.CurrentPlayers.Select(x => new { PlayerName = x.Key, Points = x.Value.Points })
									}
								}, _jsonSerializerSettings))),
								WebSocketMessageType.Text,
								true,
								CancellationToken.None);
							}
							else
							{
								var playerAlreadyAnswered = (game.GameState == QuizGameState.QuestionAnswered || !string.IsNullOrEmpty(quizPlayer.AnswerId));
								var currentQuestion = game.CurrentQuestions[game.CurrentQuestionIndex < game.CurrentQuestions.Count ? game.CurrentQuestionIndex : (game.CurrentQuestions.Count - 1)];

								await socketPlayerConnection.Socket.SendAsync(
								new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
								{
									Type = "reconnect",
									Value = new
									{
										ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
										InviteCode = socketPlayerConnection.GameId,
										GameState = "CategoryInGame",
										GameStateSub = game.GameState.ToString(),
										PlayerName = socketPlayerConnection.Username,
										LobbyMode = game.LobbyMode,
										LobbyPlayers = game.CurrentPlayers.Select(x => new
										{
											PlayerName = x.Key,
											IsOwner = x.Value.IsOwner,
											IsReady = x.Value.IsReady,
											AnswerStatus = x.Value.AnswersStatus,
											AnswerId = playerAlreadyAnswered ? x.Value.AnswerId : string.Empty,
										}),
										Settings = new
										{
											RoundCount = game.MaxCategoriesCount,
											QuestionsPerCategory = game.QuestionsPerCategoryCount,
											TimeSecondForQuestion = game.TimeSecondForQuestion,
											CategoryPerSelectionCount = game.CategoryPerSelectionCount,
											GameMode = (int)game.GameMode,
											ExcludedCategoriesList = game.ExcludedCategoriesList
										},
										CorrentAnswerId = playerAlreadyAnswered ? currentQuestion.CorrectAnswer : "",
										QuestionAnswerId = quizPlayer.AnswerId,
										Category = game.CurrentCategory,
										PreloadImages = game.CurrentQuestions.Select(x => x.Image).Where(x => !string.IsNullOrEmpty(x)),
										QuestionsCount = game.CurrentQuestions.Count,
										QuestionIndex = game.CurrentQuestionIndex,
										QuestionData = new
										{
											Id = currentQuestion.Id,
											Text = currentQuestion.Text,
											Image = currentQuestion.Image,
											QuestionStartTime = game.QuestionAnsweringTime,
											Answers = game.CurrentQuestionsAnswers.Select(x => new { Id = x.Id, Text = x.Text })
										}
									}
								}, _jsonSerializerSettings))),
								WebSocketMessageType.Text,
								true,
								CancellationToken.None);
							}
						}
						else
							return;

						quizPlayer.Disconnected = false;
						socketPlayerConnection.SocketAuthorized = true;
					}
				}
			}
			catch { }
		}

		public async Task Connection(WebSocket socket)
		{
			var socketPlayerConnection = new WebsocketPlayerConnection()
			{
				Socket = socket,
				SocketAuthorized = false,
				Username = string.Empty,
				GameId = string.Empty,
				LastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			};

			WebSocketReceiveResult receiveResult = null;

			try
			{
				var buffer = new byte[1024 * 8];
				receiveResult = await socket.ReceiveAsync(
				   new ArraySegment<byte>(buffer), CancellationToken.None);

				while (!receiveResult.CloseStatus.HasValue)
				{
					//We should handle it but in this project it should not happend
					//
					if (!receiveResult.EndOfMessage)
						throw new Exception("Message too big");

					var data = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

					if (data == "#1")
					{
						socketPlayerConnection.LastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

						await socketPlayerConnection.Socket.SendAsync(
								new ArraySegment<byte>(Encoding.UTF8.GetBytes("#2")),
								WebSocketMessageType.Text,
								true,
								CancellationToken.None);
					}
					else
					{
						try
						{
							var packet = JsonConvert.DeserializeObject<WebSocketPacket>(data);
							await HandlePacket(socketPlayerConnection, JsonConvert.DeserializeObject<WebSocketPacket>(data));
						}
						catch { }

						if (!socketPlayerConnection.SocketAuthorized)
						{
							await socket.CloseOutputAsync(
									WebSocketCloseStatus.NormalClosure,
									string.Empty,
									CancellationToken.None);
							return;
						}
					}

					receiveResult = await socket.ReceiveAsync(
						new ArraySegment<byte>(buffer), CancellationToken.None);
				}
			}
			catch { }

			if (_webSocketConnectionManager.RemoveSocket(socket, socketPlayerConnection.GameId, socketPlayerConnection.Username))
			{
				var game = _gamesService.FindGame(socketPlayerConnection.GameId);
				if (game != null)
				{
					if (game.CurrentPlayers.TryGetValue(socketPlayerConnection.Username, out var gamePlayer))
					{
						if (!gamePlayer.Disconnected)
						{
							if (socket.State == WebSocketState.CloseReceived)
								gamePlayer.LastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 40000;
							else
								gamePlayer.LastPingTime = socketPlayerConnection.LastPingTime;
						}

						gamePlayer.Disconnected = true;
					}
				}
			}

			if (socket.State != WebSocketState.Open &&
				socket.State != WebSocketState.CloseReceived)
				return;

			if (receiveResult == null || !receiveResult.CloseStatus.HasValue)
				return;

			await socket.CloseAsync(
				 receiveResult.CloseStatus.Value,
				 receiveResult.CloseStatusDescription,
				 CancellationToken.None);
		}
	}
}
