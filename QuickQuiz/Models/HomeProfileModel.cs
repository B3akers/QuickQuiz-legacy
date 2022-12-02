using QuickQuiz.Dto;

namespace QuickQuiz.Models
{
	public class HomeProfileModel
	{
		public bool AccountNotFound { get; set; }
		public bool AccountPrivate { get; set; }

		public AccountDTO Account { get; set; }
	}
}
