namespace BudgetAnalyzer
{
    public class EntryKey
    {
        public EntryKey(int sortOrder, string name)
        {
            SortOrder = sortOrder;
            Name = name;
        }

        public string Name { get; set; }
        public int SortOrder { get; set; }
    }
}
