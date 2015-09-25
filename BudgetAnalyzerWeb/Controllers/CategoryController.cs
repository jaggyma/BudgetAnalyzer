using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BudgetAnalyzer;
using BudgetAnalyzerWeb.Models;

namespace BudgetAnalyzerWeb.Controllers
{
	public class CategoryController : Controller
	{
		[HttpGet]
		public ActionResult Index()
		{
			ViewBag.Title = "Category";

			Dictionary<EntryKey, List<string>> categories = new Analyzer().GetPatterns();

			CategoryList catList = new CategoryList();
			catList.List = new List<Category>();
			catList.List.AddRange(categories.Keys.Select(f => new Category {Name = f.Name}));

			return View(catList);
		}

		public JsonResult GetPatterns(string selectedCategory)
		{
			Dictionary<EntryKey, List<string>> categories = new Analyzer().GetPatterns();

			if (categories.Keys.Count(c => c.Name == selectedCategory) > 0)
			{
				EntryKey key = categories.Keys.First(c => c.Name == selectedCategory);
				return Json(categories[key].ToArray(), JsonRequestBehavior.AllowGet);
			}

			return Json(new List<string>(), JsonRequestBehavior.AllowGet);
		}

		public JsonResult AddPattern(string selectedCategory, string newItem)
		{
			Dictionary<EntryKey, List<string>> categories = new Analyzer().GetPatterns();
			
			if (categories.Keys.Count(c => c.Name == selectedCategory) > 0)
			{
				EntryKey key = categories.Keys.First(c => c.Name == selectedCategory);
				categories[key].Add(newItem);

				new Analyzer().SavePatterns(categories);
			}

			return Json("OK", JsonRequestBehavior.AllowGet);
		}

		public JsonResult RemovePattern(string selectedCategory, string itemToRemove)
		{
			Dictionary<EntryKey, List<string>> categories = new Analyzer().GetPatterns();

			if (categories.Keys.Count(c => c.Name == selectedCategory) > 0)
			{
				EntryKey key = categories.Keys.First(c => c.Name == selectedCategory);
				categories[key].Remove(itemToRemove);

				new Analyzer().SavePatterns(categories);
			}

			return Json("OK", JsonRequestBehavior.AllowGet);
		}
	}
}