using System.Threading.Tasks;

namespace QuickQuiz.Interfaces
{
	public interface ICdnUploader
	{
		public Task<string> UploadFileAsync(string urlFilePath);
		public Task DeleteFile(string cdnPath);
		public Task<string> UploadBase64(string base64);
	}
}
