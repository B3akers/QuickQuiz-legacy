using QuickQuiz.Dto;
using QuickQuiz.Game;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace QuickQuiz.Services
{
	public class LobbyManagerService
	{
		private readonly ConcurrentDictionary<string, LobbyBase> _activeLobbies = new ConcurrentDictionary<string, LobbyBase>();

		public LobbyBase GetLobby(string lobbyId)
		{
			if (string.IsNullOrEmpty(lobbyId))
				return null;

			if (_activeLobbies.TryGetValue(lobbyId, out var lobby))
				return lobby;

			return null;
		}

		public LobbyBase CreateLobby(AccountDTO account)
		{
			return null;
		}

		public Task LobbyTick()
		{
			return Task.CompletedTask;
		}
	}
}
