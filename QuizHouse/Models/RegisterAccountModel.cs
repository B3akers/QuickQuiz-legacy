using System.ComponentModel.DataAnnotations;

namespace QuizHouse.Models
{
	public class RegisterAccountModel
	{
		[Required]
		[StringLength(25)]
		[MinLength(3)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*(?:\\ [a-zA-Z0-9]+)?$")]
		public string Username { get; set; }

		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string Password { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(64)]
		public string Email { get; set; }
	}
}
