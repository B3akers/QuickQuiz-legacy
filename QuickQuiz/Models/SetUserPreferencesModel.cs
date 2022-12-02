using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class SetUserPreferencesModel
	{
		[Required]
		[RegularExpression("^#(?:[0-9a-fA-F]{3}){1,2}$")]
		public string Color { get; set; }

		[Required]
		public bool StreamerMode { get; set; }

		[Required]
		public bool PrivateProfil { get; set; }
	}
}