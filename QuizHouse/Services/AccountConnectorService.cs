using Newtonsoft.Json.Linq;
using QuizHouse.Interfaces;
using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using QuizHouse.Dto;
using MongoDB.Driver;

namespace QuizHouse.Services
{
    public class AccountConnectorService : IAccountConnector
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly QuizService _quizService;

        public AccountConnectorService(IHttpClientFactory httpClientFactory, IConfiguration configuration, QuizService quizService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _quizService = quizService;
        }

        public async Task ConnectAccountToTwitch(AccountDTO account, string accessToken, string refreshToken, string twitchId, string twitchLogin, string twitchDisplayName)
        {
            var accounts = _quizService.GetAccountsCollection();
            await RemoveConnection(account, "Twitch");
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Push(x => x.Connections, new TwitchConnectionDTO() { Type = "Twitch", AccessToken = accessToken, RefreshToken = refreshToken, UserId = twitchId, Login = twitchLogin, Displayname = twitchDisplayName }));
        }

        public async Task RemoveConnection(AccountDTO account, string type)
        {
            var accounts = _quizService.GetAccountsCollection();
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.PullFilter(x => x.Connections, Builders<AccountConnectionDTO>.Filter.Eq(x => x.Type, type)));
        }

        public async Task<JObject> TwitchAuthorization(string code, string redirectUrl)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var values = new Dictionary<string, string>
            {
                { "client_id", _configuration["Twitch:ClientId"] },
                { "client_secret", _configuration["Twitch:ClientSecret"] },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", redirectUrl },
            };

            var content = new FormUrlEncodedContent(values);

            var message = new HttpRequestMessage();
            message.RequestUri = new Uri("https://id.twitch.tv/oauth2/token");
            message.Method = HttpMethod.Post;
            message.Content = content;

            var response = await httpClient.SendAsync(message);
            response.EnsureSuccessStatusCode();

            var stringResponse = await response.Content.ReadAsStringAsync();
            return JObject.Parse(stringResponse);
        }

        public async Task<JObject> TwitchGetUserInfo(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var message = new HttpRequestMessage();
            message.RequestUri = new Uri("https://api.twitch.tv/helix/users");
            message.Method = HttpMethod.Get;
            message.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            message.Headers.TryAddWithoutValidation("Client-Id", _configuration["Twitch:ClientId"]);

            var response = await httpClient.SendAsync(message);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();
            return JObject.Parse(stringResponse)["data"][0] as JObject;
        }
    }
}
