using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace QuickQuiz.Dto
{
	public enum ReportResultDTO
	{
		None,
		Declined,
		Approved
	};

	public enum ReportReasonDTO
	{
		OutdatedQuestion,
		WrongCategory,
		WrongAnswer,
		Other
	};

	public class QuestionReportDTO
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string QuestionId { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public List<string> Accounts { get; set; }
		[BsonRepresentation(BsonType.ObjectId)]
		public string ModeratorId { get; set; }
		public long CreationTime { get; set; }
		public int Prority { get; set; }
		public ReportReasonDTO Reason { get; set; }
		public ReportResultDTO Status { get; set; }
	}
}
