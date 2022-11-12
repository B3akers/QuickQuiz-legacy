using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace QuizHouse.Dto
{
	public class PasswordResetDTO
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string AccountId { get; set; }
		public string Key { get; set; }
		public long CreationTime { get; set; }
		public bool Used { get; set; }
	}
}
