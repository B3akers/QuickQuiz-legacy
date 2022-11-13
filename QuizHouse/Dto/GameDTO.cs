using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuizHouse.Dto
{
    public enum GameTypeDTO
    {
        Solo
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
        public GameTypeDTO GameType { get; set; }
        public GameStatusDTO GameStatus { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Players { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Categories { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Questions { get; set; }
    }
}
