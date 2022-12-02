using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickQuiz.Dto
{
	public class QuestionDTO
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public string Text { get; set; }
		public string Image { get; set; }
		public int CorrectAnswer { get; set; }
		public List<string> Answers { get; set; }

		[BsonRepresentation(BsonType.ObjectId)]
		public List<string> Categories { get; set; }

		[BsonRepresentation(BsonType.ObjectId)]
		public string Author { get; set; }
	}
}
