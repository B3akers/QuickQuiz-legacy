using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class DeleteRecordModel
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }
	};
}
