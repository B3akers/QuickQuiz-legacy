using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuizHouse.Dto
{
	public class GamePlayerQuestionSelectDTO
	{
		public int Index { get; set; }
		public long Time { get; set; }
	}

	public class GamePlayerDTO
	{
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		
		public double Points { get; set; }

		public List<GamePlayerQuestionSelectDTO> Answers = new List<GamePlayerQuestionSelectDTO>();

		[BsonRepresentation(BsonType.ObjectId)]
		public List<string> Categories = new List<string>();
	}
}
