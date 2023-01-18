using Microsoft.Extensions.Configuration;
using MongoDB.Bson.IO;
using Newtonsoft.Json.Linq;
using QuickQuiz.Interfaces;
using QuickQuiz.Utility;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuickQuiz.Services
{
	public class GithubCdnUploaderService : ICdnUploader
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;

		public GithubCdnUploaderService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
		}

        public Task DeleteFile(string cdnPath)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadBase64(string base64)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UploadFileAsync(string urlFilePath)
		{
			var fileName = urlFilePath.Substring(urlFilePath.LastIndexOf('/') + 1);
			var filePath = Path.Combine("wwwroot", "uploads", fileName);

			if (!File.Exists(filePath))
				return null;

			var httpClient = _httpClientFactory.CreateClient();
			fileName = $"{Randomizer.RandomReadableString(4)}_{fileName}";

			var commiter = new JObject();
			commiter["name"] = "Quiz-House";
			commiter["email"] = _configuration["Github:Email"];

			var jsonContent = new JObject();
			jsonContent["message"] = "";
			jsonContent["content"] = Convert.ToBase64String(await File.ReadAllBytesAsync(filePath));
			jsonContent["committer"] = commiter;

			var repoPath = _configuration["Github:RepoPath"];

			var message = new HttpRequestMessage();
			message.RequestUri = new Uri($"https://api.github.com/repos/{repoPath}/contents/images/{fileName}");
			message.Method = HttpMethod.Put;
			message.Headers.TryAddWithoutValidation("User-Agent", "QuickQuiz/github");
			message.Headers.TryAddWithoutValidation("Authorization", $"token {_configuration["Github:Token"]}");
			message.Content = new StringContent(jsonContent.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8, "application/json");

			var response = await httpClient.SendAsync(message);
			response.EnsureSuccessStatusCode();

			File.Delete(filePath);

			return $"https://raw.githubusercontent.com/{repoPath}/main/images/{fileName}";
		}
	}
}