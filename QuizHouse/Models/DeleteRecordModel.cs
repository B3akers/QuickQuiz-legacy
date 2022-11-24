using System.ComponentModel.DataAnnotations;

namespace QuizHouse.Models
{
	public class DeleteRecordModel
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }
	};
}
