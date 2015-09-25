using System.Collections.Generic;
using System.ComponentModel;

namespace BudgetAnalyzerWeb.Models
{
	public class CategoryList
	{
		[DisplayName("Category")]
		public string SelectedCategory { get; set; }

		public List<Category> List { get; set; }
	}
}