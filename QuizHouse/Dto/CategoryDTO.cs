using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Dto
{
	public class CategoryDTO
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public string Label { get; set; }
		public string Color { get; set; }
		public string Icon { get; set; }
		public int QuestionCount { get; set; }
	}
}
