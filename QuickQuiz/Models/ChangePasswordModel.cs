using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class ChangePasswordModel
	{
		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string CurrentPassword { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }
	}
}
