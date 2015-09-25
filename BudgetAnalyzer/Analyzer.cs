using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Office.Interop.Excel;

namespace BudgetAnalyzer
{
    public class Analyzer
    {
	    private static DateTime from = DateTime.MaxValue;
		private static DateTime to = DateTime.MinValue;

		private static Dictionary<EntryKey, List<string>> InternalGetPatterns()
		{
			Dictionary<EntryKey, List<string>> patterns = new Dictionary<EntryKey, List<string>>();
			StreamReader sr = new StreamReader(@"C:\_Docs\BudgetAnalyzer2\Docs\patterns.txt");
			string line;
			int sortOder = 1;
			while ((line = sr.ReadLine()) != null)
			{
				int pos = line.IndexOf(":", StringComparison.InvariantCulture);
				string categoryName = line.Substring(0, pos);
				string[] subtextList = line.Substring(pos + 1).Split('#');
				patterns.Add(new EntryKey(sortOder, categoryName), subtextList.ToList());

				sortOder++;
			}

			sr.Close();

			return patterns;
		}

		public void SavePatterns(Dictionary<EntryKey, List<string>> categories)
		{
			using (StreamWriter sw = new StreamWriter("C:\\temp\\patterns.txt"))
			{
				foreach (EntryKey key in categories.Keys)
				{
					string line = key.Name + ":" + String.Join("#", categories[key]);
					sw.WriteLine(line);
				}

				sw.Close();
			}
		}
		
	    // private const string _Csvfile = @"C:\Users\jaggy\Downloads\Konto_2015.csv";
	    
		private static readonly string[] _csvfiles = 
			{
				//@"C:\_Docs\BudgetAnalyzer2\Docs\Konto_2014.csv",
				@"C:\_Docs\BudgetAnalyzer2\Docs\Konto_2015.csv"
			};

		private static readonly string[] _pdffiles = 
			{
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908012015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908022015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908032015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908042015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908052015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908062015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908072015.pdf",
				@"C:\_Docs\BudgetAnalyzer2\Docs\000000000609437770908082015.pdf"
			};


		public static void Main(string[] args)
		{
			List<Entry> entries = ReadCembraPdfs(_pdffiles);
			// List<Entry> entries = ReadCembraCsv(@"C:\temp\tmp2\cc1414484476802.xls");

			Dictionary<EntryKey, List<string>> patterns = InternalGetPatterns();

			Dictionary<string, Result> result = new Dictionary<string, Result>();
			MatchEntries(entries, patterns, result);
			ShowUnmatchedEntries(entries);

			WriteResults(entries, result, @"C:\temp\summary.txt");
			//WriteResultsPerMonth(entries, "Einkauf");
		}

		private static List<Entry> ReadCembraPdf(string file)
	    {
		    List<Entry> entries = new List<Entry>();

		    PdfReader pdfReader = new PdfReader(file);
		    for (int page = 1; page <= pdfReader.NumberOfPages; page++)
		    {
			    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
			    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

			    currentText =
				    Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.GetEncoding("ISO-8859-1"), Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
			    string[] lines = currentText.Split('\n');
			    bool startEntries = false;
			    foreach (string line in lines)
			    {
				    if (startEntries)
				    {
					    if (line.StartsWith("Kontonummer") || line.StartsWith("Diverses"))
					    {
						    break;
					    }

					    string normalizedline = line.Replace(" ", "");

					    DateTime date;
						if (DateTime.TryParse(normalizedline.Substring(0, 10), out date))
					    {
						    string currency = "CHE";
							if (normalizedline.Contains("DEU"))
						    {
							    currency = "DEU";
						    }
							else if (normalizedline.Contains("GBR"))
						    {
							    currency = "GBR";
						    }
							else if (normalizedline.Contains("LUX"))
							{
								currency = "LUX";
							}
							else if (normalizedline.Contains("FRA"))
							{
								currency = "FRA";
							}

						    int currencyPos = normalizedline.IndexOf(currency, StringComparison.InvariantCulture);

						    if (currencyPos <= 0)
						    {
							    continue;
						    }

							string text = normalizedline.Substring(20, currencyPos - 20);
							double amount = Double.Parse(normalizedline.Substring(currencyPos + 3));

						    entries.Add(new Entry
						    {
							    Amount = -1*amount,
							    Date = date,
							    Text = text
						    });
					    }
				    }

				    if (line.StartsWith("Einkaufs-Datum"))
				    {
					    startEntries = true;
				    }
			    }
		    }

		    pdfReader.Close();

			return entries;
	    }

	    private List<Entry> GetAllEntries()
	    {
			List<Entry> entries = ReadBankCsvs(_csvfiles);
			entries.AddRange(ReadCembraPdfs(_pdffiles));

		    return entries;
	    }

	    public Dictionary<EntryKey, List<string>> GetPatterns()
	    {
		    return InternalGetPatterns();
	    }

	    public List<Result> GetPatternsWithCount()
		{
			List<Entry> entries = GetAllEntries();
			Dictionary<EntryKey, List<string>> patterns = InternalGetPatterns();
			Dictionary<string, Result> result = new Dictionary<string, Result>();
			MatchEntries(entries, patterns, result);
		    result.Remove("Ignore");

			return result.Values.ToList();
		}

        private static void ShowUnmatchedEntries(List<Entry> entries)
        {
			Console.WriteLine("Unmatched entries: {0}", entries.Count(e => e.IsMatched() == false
				&& !e.Text.Contains("E-Banking Sammelauftrag aus Einzelzahlungen")
				&& !e.Text.Contains("E-Banking Sammelauftrag aus Daueraufträgen")));

            foreach(Entry entry in entries.Where(e => e.IsMatched() == false 
				&& !e.Text.Contains("E-Banking Sammelauftrag aus Einzelzahlungen")
				&& !e.Text.Contains("E-Banking Sammelauftrag aus Daueraufträgen")))
            {
				Console.WriteLine("Unmatched: " + entry.Text);
            }

			Console.WriteLine();
        }

		private static void WriteResults(List<Entry> entries, Dictionary<string, Result> result, string resultfile)
		{
			StreamWriter sw = new StreamWriter(resultfile, false, Encoding.UTF8);

			sw.WriteLine("From: {0}, To: {1}", from.ToShortDateString(), to.ToShortDateString());

			double expenses = 0;

			double income = result.ContainsKey("Lohn") ? result["Lohn"].Sum : 0;

			foreach (string patternKey in result.Keys)
			{
				Console.WriteLine("{0}: \t{1} \t{2}", patternKey, result[patternKey].Sum, result[patternKey].Count);
				sw.WriteLine("{0}: \t{1} \t{2}", patternKey, result[patternKey].Sum, result[patternKey].Count);
				List<Entry> matchingEntries = entries.Where(e => e.MatchKey == patternKey).ToList();
				foreach (Entry entry in matchingEntries)
				{
					sw.WriteLine("   {0}: \t{1} \t{2}", entry.Date, entry.Amount, entry.Text);
				}

				expenses += result[patternKey].Sum;
			}

			sw.WriteLine("Total income: {0}", income);
			sw.WriteLine("Total expenses: {0}", expenses);

			sw.Close();
		}

		public List<Result> GetResultsPerMonth(string patternKey)
		{
			List<Entry> entries = GetAllEntries();
			Dictionary<EntryKey, List<string>> patterns = InternalGetPatterns();
			Dictionary<string, Result> tmpResult = new Dictionary<string, Result>();
			MatchEntries(entries, patterns, tmpResult);
			tmpResult.Remove("Ignore");

			DateTime minDate = entries.Min(e => e.Date);
			DateTime maxDate = entries.Max(e => e.Date);
			DateTime currentDate = minDate;

			List<Result> result = new List<Result>();
			while (currentDate < maxDate)
			{
				double sum = entries.Where(e => e.MatchKey == patternKey && e.Date.Month == currentDate.Month && e.Date.Year == currentDate.Year).Sum(e => e.Amount);
				result.Add(new Result()
				{
					Category = patternKey,
					Count = 1,
					Sum = Math.Abs(sum),
					SumMoney = Math.Abs(sum).ToString("F", CultureInfo.InvariantCulture),
					Unit = currentDate.ToString("MMMM")
				});

				currentDate = currentDate.AddMonths(1);
			}

			return result;
		}

        private static void MatchEntries(List<Entry> entries, Dictionary<EntryKey, List<string>> patterns, Dictionary<string, Result> result)
        {
            foreach (Entry entry in entries)
            {
	            if (entry.IsSammelauftrag && entry.Text.Contains("Sammelauftrag"))
	            {
					continue;
	            }

	            if (from.CompareTo(entry.Date) > 0)
	            {
		            from = entry.Date;
	            }

				if (to.CompareTo(entry.Date) < 0)
				{
					to = entry.Date;
				}

                foreach (EntryKey patternKey in patterns.Keys.OrderBy(e => e.SortOrder))
                {
                    bool matched = false;
                    foreach (string pattern in patterns[patternKey])
                    {
                        if (entry.Text.Contains(pattern))
                        {
                            if (!result.ContainsKey(patternKey.Name))
                            {
								result.Add(patternKey.Name, new Result() { Category = patternKey.Name });
                            }

	                        result[patternKey.Name].IsIncome = entry.Amount > 0;
                            result[patternKey.Name].Sum += Math.Abs(entry.Amount);
                            result[patternKey.Name].Count ++;
                            entry.MatchKey = patternKey.Name;

                            // Only 1 should match
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        break;
                    }
                }

                // Gutschriften
                if (entry.Amount > 0 && string.IsNullOrEmpty(entry.MatchKey))
                {
                    if (!result.ContainsKey("Gutschriften"))
                    {
						result.Add("Gutschriften", new Result { Category = "Gutschriften" });
                    }

                    result["Gutschriften"].Sum += Math.Abs(entry.Amount);
	                result["Gutschriften"].IsIncome = true;
                    result["Gutschriften"].Count++;
                    entry.MatchKey = "Gutschriften";
                }

                // Unklare Einträge
                if (string.IsNullOrEmpty(entry.MatchKey))
                {
                    if (!result.ContainsKey("Unmatched"))
                    {
                        result.Add("Unmatched", new Result { Category = "Unmatched" });
                    }

					result["Unmatched"].IsIncome = entry.Amount > 0;
                    result["Unmatched"].Sum += Math.Abs(entry.Amount);
                    result["Unmatched"].Count++;
                    entry.MatchKey = "Unmatched";
                }
            }
        }

	    private static List<Entry> ReadCembraCsv(string file)
	    {
		    List<Entry> entries = new List<Entry>();
		    try
		    {
			    string filename = "C:\\temp\\cc1414484476802.xls";
			    Application _excelApp = new Application();
			    Workbook workBook = _excelApp.Workbooks.Open(filename,
				    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
				    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
				    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
				    Type.Missing, Type.Missing);
				
				Worksheet sheet = (Worksheet)workBook.Sheets["Report"];
				Range excelRange = sheet.UsedRange;
				object[,] valueArray = (object[,])excelRange.get_Value(XlRangeValueDataType.xlRangeValueDefault);

				for (int y = 8; y < valueArray.GetLength(0); y++)
				{
					string date = valueArray[y, 1].ToString();
					string desc = valueArray[y, 4].ToString();
					string amount = valueArray[y, 6].ToString().Replace("CHF", string.Empty);
					Entry entry = new Entry
					{
						Amount = Double.Parse(amount),
						Date = DateTime.Parse(date),
						Text = desc
					};
					entries.Add(entry);
				}

				workBook.Close(false, filename, null);
				Marshal.ReleaseComObject(workBook);
		    }
		    catch (Exception x)
		    {
			    Console.WriteLine(x.ToString());
		    }

		    return entries;
	    }

	    private static List<Entry> ReadBankCsvs(string[] files)
	    {
			List<Entry> entries = new List<Entry>();
		    foreach (string file in files)
		    {
			    entries.AddRange(ReadBankCsv(file));
		    }

			return entries;
	    }

		private static List<Entry> ReadCembraPdfs(string[] files)
		{
			List<Entry> entries = new List<Entry>();
			foreach (string file in files)
			{
				entries.AddRange(ReadCembraPdf(file));
			}

			return entries;
		}

	    private static List<Entry> ReadBankCsv(string file)
        {
            List<Entry> entries = new List<Entry>();
            Entry lastEntry = new Entry();
            using (StreamReader sr = new StreamReader(file, Encoding.GetEncoding("iso-8859-1")))
            {
                string line;
                int lineCount = 1;
                while ((line = sr.ReadLine()) != null)
                {
                    // Skip header
                    if (lineCount == 1)
                    {
                        lineCount++;
                        continue;
                    }

                    string[] data = line.Split(';');
                    DateTime date = !string.IsNullOrEmpty(data[0]) ? DateTime.Parse(data[0]) : DateTime.MinValue;
                    string text = data[1].Replace('\t',' ').Trim();
                    double amount = !string.IsNullOrEmpty(data[2]) ? Double.Parse(data[2]) : 0;

                    if (date == DateTime.MinValue)
                    {
                        lastEntry.Text += " / " + text;
                        lastEntry.IsSammelauftrag = true;

	                    if (lastEntry.Text.Contains("Sammelauftrag"))
	                    {
		                    Entry subEntry = new Entry
		                    {
			                    Date = lastEntry.Date,
			                    Text = text,
			                    Amount = GetAmountFromSubEntry(text)
		                    };
		                    entries.Add(subEntry);
	                    }

	                    lineCount++;
                        continue;
                    }

                    lastEntry = new Entry {Date = date, Text = text, Amount = amount};
                    entries.Add(lastEntry);
                   
                    lineCount++;
                }

				sr.Close();
            }

            return entries;
        }

	    private static double GetAmountFromSubEntry(string text)
	    {
		    if (text.Contains("CHF"))
		    {
			    return -1 * Double.Parse(text.Substring(text.IndexOf("CHF")+3).Replace(")", ""));
		    }

		    throw new ArgumentException("Could not find amount in '" + text + "'");
	    }

	    private static void ReadSammelauftrag(StreamReader sr, List<Entry> entries, double amount)
        {
            Entry result = new Entry {Amount = amount, Text = "Sammelauftrag: "};
            string sammelAuftragLine;
            while ((sammelAuftragLine = sr.ReadLine()) != null)
            {
                string[] data = sammelAuftragLine.Split(';');
                
                string text = data[1];

                if (string.IsNullOrEmpty(data[0])) // Data for Sammelauftrag
                {
                    result.Text += " / " + text;
                }
                else
                {
                    entries.Add(result);

                    DateTime date = !string.IsNullOrEmpty(data[0]) ? DateTime.Parse(data[0]) : DateTime.MinValue;
                    double regularEntryAmount = !string.IsNullOrEmpty(data[2]) ? Double.Parse(data[2]) : 0;

                    entries.Add(new Entry {Date = date, Text = text, Amount = regularEntryAmount});
                    break;
                }
            }

        }

	    public List<Entry> GetListForCategoryAndMonth(string category, int month)
	    {
			List<Entry> entries = GetAllEntries();
			Dictionary<EntryKey, List<string>> patterns = InternalGetPatterns();
			Dictionary<string, Result> tmpResult = new Dictionary<string, Result>();
			MatchEntries(entries, patterns, tmpResult);
		    tmpResult.Remove("Ignore");

			DateTime minDate = entries.Min(e => e.Date);
			DateTime maxDate = entries.Max(e => e.Date);
			DateTime currentDate = minDate;

		    int monthCounter = 1;
			while (currentDate < maxDate)
			{
				if (monthCounter == month)
				{
					break;
				}

				currentDate = currentDate.AddMonths(1);
				monthCounter++;
			}

		    return entries.Where(e => e.MatchKey == category && e.Date.Month == currentDate.Month && e.Date.Year == currentDate.Year).ToList();
	    }

	   
    }
}
