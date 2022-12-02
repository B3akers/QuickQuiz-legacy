using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class SoloGameModel
	{
		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		public string CategoryId { get; set; }
	}
}
