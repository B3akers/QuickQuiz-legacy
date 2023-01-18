using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class LobbyCreateModel
	{
		[Required]
		public bool TwitchLobby { get; set; }
	}
}
