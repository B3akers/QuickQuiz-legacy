﻿using Newtonsoft.Json.Linq;
using QuizHouse.Dto;
using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
	public interface IAccountConnector
	{
		public Task<JObject> TwitchAuthorization(string code, string redirectUrl);
		public Task<JObject> TwitchGetUserInfo(string accessToken);
		public Task ConnectAccountToTwitch(AccountDTO account, string accessToken, string refreshToken, string twitchId, string twitchLogin, string twitchDisplayName);
		public Task RemoveConnection(AccountDTO account, string type);
	}
}
