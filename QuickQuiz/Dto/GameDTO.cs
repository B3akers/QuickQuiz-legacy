using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuickQuiz.Dto
{
    public enum GameTypeDTO
    {
        Solo,
        Custom
    };

    public enum GameStatusDTO
    {
        Running,
        Finished,
        Aborted
    };

    public class GameDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public long CreationTime { get; set; }
        public GameTypeDTO GameType { get; set; }
        public GameStatusDTO GameStatus { get; set; }
        public List<GamePlayerDTO> Players { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Categories { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Questions { get; set; }
    }
}
