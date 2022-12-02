using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class RequestPasswordResetModel
	{
		[Required]
		[EmailAddress]
		[StringLength(64)]
		public string Email { get; set; }
	}
}
