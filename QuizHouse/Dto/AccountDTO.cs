using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Dto
{
    [BsonKnownTypes(typeof(TwitchConnectionDTO))]
    public abstract class AccountConnectionDTO
    {
        public string Type { get; set; }
    }

    public sealed class TwitchConnectionDTO : AccountConnectionDTO
    {
        public string UserId { get; set; }
        public string Login { get; set; }
        public string Displayname { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class AccountDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string LastGameId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string CustomColor { get; set; }
        public string UserColor { get; set; }
        public string BanReason { get; set; }
        public bool ProfilPrivate { get; set; }
        public bool ShowBanReason { get; set; }
        public bool StreamerMode { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public long CreationTime { get; set; }
        public long LastPasswordChange { get; set; }
        public long LastEmailConfirmSend { get; set; }
        public long LastEmailPasswordSend { get; set; }
        public int ReportWeight { get; set; }
        public int ActiveReports { get; set; }
        public List<AccountConnectionDTO> Connections { get; set; }
    }
}
