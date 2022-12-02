using QuickQuiz.Dto;
using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class ReportQuestionModel
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[Range(0, (int)ReportReasonDTO.Other)]
		public ReportReasonDTO ReportReason { get; set; }
	};
}
