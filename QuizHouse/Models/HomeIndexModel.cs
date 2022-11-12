using QuizHouse.Dto;
using System.Collections.Generic;

namespace QuizHouse.Models
{
	public class HomeIndexModel
	{
		public List<CategoryDTO> Categories { get; set; }
		public NavBarModel ModelNavBar { get; set; }
	}
}
