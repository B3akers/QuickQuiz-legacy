using QuizHouse.Dto;
using System.Collections.Generic;

namespace QuizHouse.Models
{
	public class UserSettingsModel
	{
		public List<AccountConnectionDTO> AccountConnections { get; set; }
		public NavBarModel ModelNavBar { get; set; }
		public string Username { get; set; }
	}
}
