using System.ComponentModel.DataAnnotations;

namespace QuizHouse.Models
{
	public class SoloGameModel
	{
		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		public string CategoryId { get; set; }
	}
}
