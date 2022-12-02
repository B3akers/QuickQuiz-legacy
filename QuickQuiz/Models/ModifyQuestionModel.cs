using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickQuiz.Models
{
	public class ModifyQuestionModel
	{
		[RegularExpression("^[a-f\\d]{24}$")]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(350)]
		public string Label { get; set; }

		[MaxLength(80)]
		public string Image { get; set; }

		[RegularExpression("^[a-f\\d]{24}$")]
		public string Author { get; set; }

		[Required]
		[Range(0, 3)]
		public int CorrectAnswer { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer0 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer1 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer2 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(250)]
		public string Answer3 { get; set; }

		[Required]
		[MinLength(1)]
		[MaxLength(8)]
		public List<string> SelectedCategories { get; set; }
	};
}
