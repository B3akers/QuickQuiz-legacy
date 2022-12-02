using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class ModifyUserProfilModel
	{
		[Required]
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }
		[StringLength(25)]
		[MinLength(3)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_]*(?:\\ [a-zA-Z0-9]+)?$")]
		public string UserName { get; set; }
		[EmailAddress]
		[StringLength(64)]
		public string UserEmail { get; set; }
		public bool? IsAdmin { get; set; }
		public bool? IsModerator { get; set; }
		public bool? EmailConfirmed { get; set; }
		public bool? StreamerMode { get; set; }
		public bool? PrivateProfile { get; set; }
		[RegularExpression("^#(?:[0-9a-fA-F]{3}){1,2}$")]
		public string CustomColor { get; set; }
		[RegularExpression("^#(?:[0-9a-fA-F]{3}){1,2}$")]
		public string UserColor { get; set; }
		public int? ReportWeight { get; set; }
	}
}
