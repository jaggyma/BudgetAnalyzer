using System;
using System.Collections.Generic;
using System.Web.Mvc;
using BudgetAnalyzer;

namespace BudgetAnalyzerWeb.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Title = "Overview";

			return View();
		}

		public JsonResult GetPatterns()
		{
			Analyzer analyzer = new Analyzer();
			List<Result> categoryList = analyzer.GetPatternsWithCount();

			return Json(categoryList, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetOverviewPerMonth(string category)
		{
			Analyzer analyzer = new Analyzer();
			List<Result> entries = analyzer.GetResultsPerMonth(category);

			return Json(entries, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetListForCategoryAndMonth(string category, string month)
		{
			Analyzer analyzer = new Analyzer();
			List<Entry> entries = analyzer.GetListForCategoryAndMonth(category, Int32.Parse(month));

			return Json(entries, JsonRequestBehavior.AllowGet);
		}
	}
}