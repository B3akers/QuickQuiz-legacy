using Microsoft.Extensions.Configuration;
using QuickQuiz.Interfaces;
using QuickQuiz.Utility;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Linq;

namespace QuickQuiz.Services
{
    public class FileCdnUploaderService : ICdnUploader
    {
        private readonly IConfiguration _configuration;

        public FileCdnUploaderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task DeleteFile(string cdnPath)
        {
            if (string.IsNullOrEmpty(cdnPath))
                return Task.CompletedTask;

            var start = cdnPath.IndexOf(_configuration["CdnServer:Url"]);
            if (start != 0)
                return Task.CompletedTask;

            var fileName = cdnPath.Substring(_configuration["CdnServer:Url"].Length);
            var filePath = Path.Combine(_configuration["CdnServer:Path"], fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }

        public async Task<string> UploadBase64(string base64)
        {
            var startIndex = base64.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return null;

            var extStart = base64.IndexOf('/') + 1;
            var ext = base64.Substring(extStart, base64.IndexOf(';', extStart) - extStart);
            if (ext.Any(x => char.IsLetterOrDigit(x) == false))
                return null;

            var fileName = $"{Randomizer.RandomReadableString(4)}_{Path.GetRandomFileName()}.{ext}";
            await File.WriteAllBytesAsync(Path.Combine(_configuration["CdnServer:Path"], fileName), Convert.FromBase64String(base64.Substring(startIndex + 7)));

            return $"{_configuration["CdnServer:Url"]}{fileName}";
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
