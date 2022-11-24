using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuizHouse.Dto
{
	public enum QuestionRequestResult
	{
		None,
		Accepted,
		Declined
	}
	public class QuestionRequestDTO
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public string Text { get; set; }
		public string Image { get; set; }
		public long CreationTime { get; set; }
		public int Prority { get; set; }
		public int CorrectAnswer { get; set; }
		public List<string> Answers { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public List<string> Categories { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string Author { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string ModeratorId { get; set; }
		public QuestionRequestResult Result { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string QuestionId { get; set; }
	}
}
