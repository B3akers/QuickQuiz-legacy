using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Dto
{
    public abstract class AccountConnection
    {
        public string Type { get; set; }
    }

    public sealed class TwitchConnection : AccountConnection
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }

    public class AccountDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public List<AccountConnection> Connections { get; set; }
    }
}
