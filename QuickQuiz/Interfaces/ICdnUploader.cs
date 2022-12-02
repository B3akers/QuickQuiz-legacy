using System.Threading.Tasks;

namespace QuickQuiz.Interfaces
{
	public interface ICdnUploader
	{
		public Task<string> UploadFileAsync(string urlFilePath);
	}
}
