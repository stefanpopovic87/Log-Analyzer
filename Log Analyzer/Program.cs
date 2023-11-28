using LogAnalyzer;

class Program
{
    private static readonly string _logsFolder = "Logs";
    private static readonly string _searchPatter = "*.log";
    private static readonly string _targetColumnName = "c-ip";

    static async Task Main()
    {
        try
        {
            string[] files = FileReader.GetFiles(_logsFolder, _searchPatter);
            Dictionary<string, IEnumerable<LogItem>> items = new();
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                var task = LogAnalyzer.LogAnalyzer.AnalyzeLogFileAsync(file, _targetColumnName)
                    .ContinueWith(t => items.Add(file, t.Result));

                tasks.Add(task);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            var sortedItems = LogAnalyzer.LogAnalyzer.SortItems(items);

            FileWriter.LogData(sortedItems);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

