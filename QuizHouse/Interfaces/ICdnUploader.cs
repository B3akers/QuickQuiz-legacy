using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
	public interface ICdnUploader
	{
		public Task<string> UploadFileAsync(string filepath);
	}
}
