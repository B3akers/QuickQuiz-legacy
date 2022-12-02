using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickQuiz.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickQuiz.Controllers
{
	public class WebSocketsController : ControllerBase
	{
		private WebSocketHandlerOld _webSocketHandler;

		public WebSocketsController(WebSocketHandlerOld webSocketHandler)
		{
			_webSocketHandler = webSocketHandler;
		}

		[HttpGet("/ws")]
		public async Task Get()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

				await _webSocketHandler.Connection(webSocket);
			}
			else
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
			}
		}
	}
}
