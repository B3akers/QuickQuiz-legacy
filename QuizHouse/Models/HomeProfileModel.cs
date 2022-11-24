using QuizHouse.Dto;

namespace QuizHouse.Models
{
	public class HomeProfileModel
	{
		public bool AccountNotFound { get; set; }
		public bool AccountPrivate { get; set; }

		public AccountDTO Account { get; set; }
	}
}
