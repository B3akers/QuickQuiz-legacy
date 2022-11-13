using System.Collections.Concurrent;

namespace QuizHouse.Game
{
    public class GameBase
    {
        public string Id { get; set; }
        public ConcurrentDictionary<string, GamePlayerBase> Players  = new ConcurrentDictionary<string, GamePlayerBase>();

        public bool PlayerInGame(string accountId)
        {
            return Players.ContainsKey(accountId);
        }
    }
}
