using System.ComponentModel.DataAnnotations;

namespace QuizHouse.Models
{
	public class RemoveAccountConnectionModel
	{
		[Required]
		[StringLength(64)]
		[MinLength(3)]
		public string ConnectionType { get; set; }
	}
}
