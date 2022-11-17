using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuizHouse.Dto
{
	public class GamePlayerSelectDTO
	{
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public long Time { get; set; }
	}

	public class GamePlayerDTO
	{
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }

		public List<GamePlayerSelectDTO> Answers = new List<GamePlayerSelectDTO>();

		[BsonRepresentation(BsonType.ObjectId)]
		public List<string> Categories = new List<string>();
	}
}
