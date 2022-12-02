using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class LoginAccountModel
	{
		[Required]
		[StringLength(64)]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }
	}
}
