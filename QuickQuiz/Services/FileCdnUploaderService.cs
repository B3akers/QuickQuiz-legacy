using Microsoft.Extensions.Configuration;
using QuickQuiz.Interfaces;
using QuickQuiz.Utility;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuickQuiz.Services
{
	public class FileCdnUploaderService : ICdnUploader
	{
		private readonly IConfiguration _configuration;

		public FileCdnUploaderService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public Task<string> UploadFileAsync(string urlFilePath)
		{
			var fileName = urlFilePath.Substring(urlFilePath.LastIndexOf('/') + 1);
			var filePath = Path.Combine("wwwroot", "uploads", fileName);

			if (!File.Exists(filePath))
				return Task.FromResult((string)null);

			fileName = $"{Randomizer.RandomReadableString(4)}_{fileName}";

			File.Copy(filePath, Path.Combine(_configuration["CdnServer:Path"], fileName), true);
			File.Delete(filePath);

			return Task.FromResult($"{_configuration["CdnServer:Url"]}{fileName}");
		}
	}
}
