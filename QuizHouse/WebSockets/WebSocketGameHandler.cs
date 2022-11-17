﻿using QuizHouse.Dto;
using QuizHouse.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuizHouse.WebSockets
{
	public class WebSocketGameHandler
	{
		private readonly GameManagerService _gameManagerService;
		private ConcurrentDictionary<string, WebSocket> _activeConnections = new ConcurrentDictionary<string, WebSocket>();

		public WebSocketGameHandler(GameManagerService gameManagerService)
		{
			_gameManagerService = gameManagerService;
		}

		public async Task Connection(AccountDTO account, WebSocket socket)
		{
			WebSocketReceiveResult receiveResult = null;

			if (_activeConnections.TryRemove(account.Id, out var oldSocket))
			{
				_gameManagerService.InvalidateSocket(account.LastGameId, account.Id, oldSocket.State);

				if (oldSocket.State == WebSocketState.Open
					|| oldSocket.State == WebSocketState.CloseReceived)
				{
					await oldSocket.CloseAsync((WebSocketCloseStatus)3001, "Another connection", CancellationToken.None);
				}
			}

			if (!_activeConnections.TryAdd(account.Id, socket))
			{
				await oldSocket.CloseAsync((WebSocketCloseStatus)3001, "Another connection", CancellationToken.None);
				return;
			}

			if (_gameManagerService.TryAssignSocketToPlayer(account.LastGameId, account.Id, socket))
			{
				var userGame = _gameManagerService.GetActiveGame(account.LastGameId);
				if (userGame != null)
				{
					await socket.SendAsync(
								new ArraySegment<byte>(userGame.GetGameStatePacket(account.Id)),
								WebSocketMessageType.Text,
								true,
								CancellationToken.None);
				}
			}

			try
			{
				var buffer = new byte[1024 * 8];
				receiveResult = await socket.ReceiveAsync(
				   new ArraySegment<byte>(buffer), CancellationToken.None);

				while (!receiveResult.CloseStatus.HasValue)
				{
					if (!receiveResult.EndOfMessage)
					{
						await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, string.Empty, CancellationToken.None);
						break;
					}

					var data = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

					if (data == "#1")
					{
						await socket.SendAsync(
								new ArraySegment<byte>(Encoding.UTF8.GetBytes("#2")),
								WebSocketMessageType.Text,
								true,
								CancellationToken.None);
					}
					else
					{

					}

					receiveResult = await socket.ReceiveAsync(
										new ArraySegment<byte>(buffer), CancellationToken.None);
				}
			}
			catch { }

			if (_activeConnections.TryRemove(account.Id, out oldSocket))
				_gameManagerService.InvalidateSocket(account.LastGameId, account.Id, oldSocket.State);

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
