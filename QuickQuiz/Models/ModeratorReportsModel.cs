using QuickQuiz.Dto;
using System.Collections.Generic;

namespace QuickQuiz.Models
{
	public class ModeratorReportsModel
	{
		public List<QuestionReportDTO> Reports { get; set; }
		public List<QuestionDTO> Questions { get; set; }
	}
}
