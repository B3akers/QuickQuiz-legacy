using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using QuizHouse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuizHouse.Controllers
{
    public class CreateGameParametrs
    {
        [Required]
        [StringLength(15)]
        [MinLength(3)]
        [RegularExpression("^[a-zA-Z0-9 _]*$")]
        public string Username { get; set; }
    }

    public class JoinGameParametrs
    {
        [Required]
        [StringLength(15)]
        [MinLength(3)]
        [RegularExpression("^[a-zA-Z0-9 _]*$")]
        public string Username { get; set; }

        [Required]
        public string InviteCode { get; set; }

        public string Token { get; set; }
    }

    public class HomeController : Controller
    {
        private GamesService _gamesService;
        private QuizService _quizService;
        private JwtTokensService _jwtTokensService;
        private IConfiguration _configuration;

        private static HttpClient _httpClient = new HttpClient();

        public HomeController(IConfiguration configuration, GamesService gamesService, JwtTokensService jwtTokensService, QuizService quizService)
        {
            _gamesService = gamesService;
            _jwtTokensService = jwtTokensService;
            _quizService = quizService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("/JoinGame")]
        public IActionResult JoinGame([FromBody] JoinGameParametrs joinGameParametrs)
        {
            if (!ModelState.IsValid)
                return Json(new { error = "invalid_username" });

            joinGameParametrs.Username = joinGameParametrs.Username.Trim();

            var gameInfo = _gamesService.FindGame(joinGameParametrs.InviteCode);

            if (gameInfo == null)
                return Json(new { error = "game_not_found" });

            if (gameInfo.GameState != QuizGameState.Lobby)
                return Json(new { error = "game_running" });

            if (gameInfo.CurrentPlayers.Count >= 100)
                return Json(new { error = "game_lobby_full" });

            if (gameInfo.LobbyMode == QuizLobbyMode.TwitchAuth)
            {
                if (string.IsNullOrEmpty(joinGameParametrs.Token) || !_jwtTokensService.ValidateToken(joinGameParametrs.Token))
                    return Json(new { error = "invalid_username_token" });

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.ReadToken(joinGameParametrs.Token) as JwtSecurityToken;
                var userNameClaim = securityToken.Claims.FirstOrDefault(x => x.Type == "username");

                joinGameParametrs.Username = userNameClaim.Value;
            }

            if (!gameInfo.CurrentPlayers.TryAdd(joinGameParametrs.Username, new QuizGamePlayer()
            {
                IsOwner = false,
                IsReady = false
            }))
                return Json(new { error = "invalid_username" });

            var token = _jwtTokensService.GenerateToken(new ClaimsIdentity(new Claim[]
            {
                  new Claim("username", joinGameParametrs.Username),
                  new Claim("game_id", joinGameParametrs.InviteCode),
            }),
            DateTime.UtcNow.AddHours(1));

            return Json(new { token = token });
        }

        [HttpGet]
        [Route("/TwitchLogin")]
        public async Task<IActionResult> TwitchLogin(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return new RedirectResult(Url.Action("Index", "Home", null, Request.Scheme), false);

                var values = new Dictionary<string, string>
                {
                    { "client_id", _configuration["Twitch:ClientId"] },
                    { "client_secret", _configuration["Twitch:ClientSecret"] },
                    { "code", code },
                    { "grant_type", "authorization_code" },
                    { "redirect_uri", Url.Action("TwitchLogin", "Home", null, Request.Scheme) },
                };

                var content = new FormUrlEncodedContent(values);

                var message = new HttpRequestMessage();
                message.RequestUri = new Uri("https://id.twitch.tv/oauth2/token");
                message.Method = HttpMethod.Post;
                message.Content = content;

                var response = await _httpClient.SendAsync(message);
                var stringResponse = await response.Content.ReadAsStringAsync();
                var jsonData = JObject.Parse(stringResponse);
                var accessToken = (string)jsonData["access_token"];

                var twitchUserMessage = new HttpRequestMessage();
                twitchUserMessage.RequestUri = new Uri("https://api.twitch.tv/helix/users");
                twitchUserMessage.Method = HttpMethod.Get;
                twitchUserMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                twitchUserMessage.Headers.TryAddWithoutValidation("Client-Id", _configuration["Twitch:ClientId"]);

                var responseTwitch = await _httpClient.SendAsync(twitchUserMessage);
                var twitchResponseText = await responseTwitch.Content.ReadAsStringAsync();
                var twitchRespone = JObject.Parse(twitchResponseText);

                var username = (string)twitchRespone["data"][0]["login"];
                var twitchUsernameToken = _jwtTokensService.GenerateToken(new ClaimsIdentity(new Claim[]
                {
                    new Claim("username", username)
                }),
                DateTime.UtcNow.AddDays(30));

                return new RedirectResult(Url.Action("Index", "Home", new { username_token = twitchUsernameToken }, Request.Scheme), false);
            }
            catch
            {
                return new RedirectResult(Url.Action("Index", "Home", null, Request.Scheme), false);
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 3600)]
        [Route("/GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            return Json(await _quizService.GetCategoriesAsync());
        }

        [HttpPost]
        [Route("/CreateGame")]
        public IActionResult CreateGame([FromBody] CreateGameParametrs createGameParametrs)
        {
            if (!ModelState.IsValid)
                return Json(new { error = "invalid_username" });

            createGameParametrs.Username = createGameParametrs.Username.Trim();

            var gameInfo = _gamesService.CreateGame();

            if (gameInfo == null)
                return Json(new { error = "internal_server_error" });

            gameInfo.Item2.OwnerName = createGameParametrs.Username;

            if (!gameInfo.Item2.CurrentPlayers.TryAdd(createGameParametrs.Username, new QuizGamePlayer()
            {
                IsOwner = true,
                IsReady = false
            }))
                return Json(new { error = "internal_server_error" });

            var token = _jwtTokensService.GenerateToken(new ClaimsIdentity(new Claim[]
                 {
                    new Claim("username", createGameParametrs.Username),
                    new Claim("game_id", gameInfo.Item1),
                 }),
                 DateTime.UtcNow.AddHours(1));

            return Json(new { token = token });
        }
    }
}
