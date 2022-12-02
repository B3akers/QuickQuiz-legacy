using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class ChangeUsernameModel
	{
		[Required]
		[StringLength(64)]
		[MinLength(6)]
		public string CurrentPassword { get; set; }

		[Required]
		[StringLength(25)]
		[MinLength(3)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*(?:\\ [a-zA-Z0-9]+)?$")]
		public string Username { get; set; }
	};
}
