using System;

namespace BudgetAnalyzer
{
    public class Entry
    {
        public DateTime Date { get; set; }
        public string Text { get; set; }
        public double Amount { get; set; }

        public bool IsSammelauftrag { get; set; }
        public string MatchKey { get; set; }

        public bool IsMatched()
        {
            return !string.IsNullOrEmpty(MatchKey);
        }
    }
}
