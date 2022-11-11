using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Dto
{
    public class EmailConfirmationDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string AccountId { get; set; }
        public string Email { get; set; }
        public string Key { get; set; }
        public long CreationTime { get; set; }
        public bool Used { get; set; }
    }
}
