using QuizHouse.Dto;
using System.Collections.Generic;

namespace QuizHouse.Models
{
	public class ModeratorReportsModel
	{
		public List<QuestionReportDTO> Reports { get; set; }
		public List<QuestionDTO> Questions { get; set; }
	}
}
