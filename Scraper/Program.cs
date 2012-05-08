using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Model;
using Core.Persistence;
using FileHelpers;
using WatiN.Core;

namespace Scraper
{
	public class Program
	{
		private static readonly string resource = "http://onlineweb.dkpto.dk/pvsonline/Patent";

		private static readonly IEnumerable<string> malenames =
			GetListFromCVS("data/drenge.csv").Select(_ => _.Trim());
		private static readonly IEnumerable<string> femalenames =
			GetListFromCVS("data/piger.csv").Select(_ => _.Trim());

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0 && args.First() == "inventors")
			{
				ExtractInventors();
			}
			else
			{
				ScrapePatents();
			}
		}

		private static void ScrapePatents()
		{
			Console.WriteLine("scraping patents...");
			foreach (var year in Enumerable.Range(2007, 5))
			{
				GetPatentsInYear(year);
			}
		}

		private static void GetPatentsInYear(int year)
		{
			using (var browser = new IE(resource))
			{
				// go to the search form
				browser.Button(Find.ByName("menu")).ClickNoWait();

				// fill out search form and submit
				browser.CheckBox(Find.ByName("brugsmodel")).Click();
				browser.SelectList(Find.ByName("datotype")).Select("Patent/reg. dato");
				browser.TextField(Find.ByName("dato")).Value = string.Format("{0}*", year);
				browser.Button(Find.By("type", "submit")).ClickNoWait();
				browser.WaitForComplete();

				// go to first patent found in search result and save it
				browser.Buttons.Filter(Find.ByValue("Vis")).First().Click();
				GetPatentFromPage(browser, year);

				// hit the 'next' button until it's no longer there
				while (GetNextPatentButton(browser).Exists)
				{
					GetNextPatentButton(browser).Click();
					GetPatentFromPage(browser, year);
				}
			}
		}

		private static void GetPatentFromPage(IE browser, int year)
		{
			var inventor = browser.TableCell(x =>
				x.PreviousSibling != null && x.PreviousSibling.Text == "Opfinder").Text;

			var patentIdAndDate = browser.TableCell(x =>
				x.PreviousSibling != null && x.PreviousSibling.Text == "Patent/reg.nr. og dato").Text;

			var patentId = patentIdAndDate.Split(',').First();

			using (var context = new Context())
			{
				var patent = new Patent
				{
					Html = browser.Html,
					InventorField = inventor,
					PatentId = patentId,
					Year = year,
				};
				context.Patents.Add(patent);
				context.SaveChanges();
			}
		}

		private static Button GetNextPatentButton(IE browser)
		{
			return browser.Button(button =>
				button.Value == "Næste" && button.ClassName == "knapanden");
		}

		private static void ExtractInventors()
		{
			Console.WriteLine("extracting inventors...");
			using (var context = new Context())
			{
				foreach (var patent in context.Patents)
				{
					var inventorStrings = Regex.Split(patent.InventorField, "[A-Z]{2},")
						.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim());

					foreach (var inventorString in inventorStrings)
					{
						var name = inventorString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).First();
						var firstname = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).First();

						Console.WriteLine(firstname);
					}
				}
			}
		}

		private static IEnumerable<string> GetListFromCVS(string filename)
		{
			var eng = new FileHelperEngine<Name>();
			return eng.ReadFile(filename).Select(_ => _.Value);
		}
	}

	[DelimitedRecord(",")]
	class Name
	{
		public string Value { get; set; }
	}
}
