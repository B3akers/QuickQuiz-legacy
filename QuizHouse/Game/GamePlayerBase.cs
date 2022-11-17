using MongoDB.Bson.Serialization.Attributes;
using QuizHouse.Dto;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace QuizHouse.Game
{
	public class GamePlayerBase : GamePlayerDTO
	{
		public string Username { get; set; }
		public string CategoryVoteId { get; set; }
		public string AnswerId { get; set; }
		public long TimeoutTime { get; set; }
		public List<Tuple<bool, long>> AnswersData = new List<Tuple<bool, long>>();
		public WebSocket AssignedSocket { get; set; }
	}
}
