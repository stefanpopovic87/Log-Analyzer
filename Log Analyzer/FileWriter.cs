namespace LogAnalyzer
{
    public static class FileWriter
    {
        public static void LogData(Dictionary<string, List<LogItem>> items)
        {
            foreach (var file in items)
            {
                Console.WriteLine($"Analyzing log file: {file.Key}");
                foreach (var logItem in file.Value)
                {
                    Console.WriteLine($"{logItem.HostName} ({logItem.Cip}) - {logItem.Counter}");
                }

                Console.WriteLine();
            }
        }
    }
}
