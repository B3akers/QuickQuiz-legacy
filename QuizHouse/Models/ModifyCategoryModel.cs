using System.ComponentModel.DataAnnotations;

namespace QuizHouse.Models
{
	public class ModifyCategoryModel
	{
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(40)]
		public string Label { get; set; }

		[Required]
		[RegularExpression("^#(?:[0-9a-fA-F]{3}){1,2}$")]
		public string Color { get; set; }

		[MinLength(3)]
		[MaxLength(80)]
		public string Icon { get; set; }

		public string IconBase64 { get; set; }
	};
}
