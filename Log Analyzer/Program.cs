using System.Net;

class Program
{
    private static Dictionary<string, string> hostnameCache = new Dictionary<string, string>();

    static async Task Main()
    {
        string logsFolder = "Logs"; // Replace with your actual folder path
        string[] logFiles = Directory.GetFiles(logsFolder, "*.log");
        string targetColumnName = "c-ip";

        foreach (var logFile in logFiles)
        {
            Console.WriteLine($"Analyzing log file: {logFile}");
            var ipHits = await AnalyzeLogFileAsync(logFile, targetColumnName);

            // Sort IP addresses by hits in descending order
            var sortedIpHits = ipHits.OrderByDescending(kv => kv.Value);

            // Output the results to the console
            foreach (var entry in sortedIpHits)
            {
                string hostName = await ResolveHostNameAsync(entry.Key);
                Console.WriteLine($"{hostName} ({entry.Key}) - {entry.Value}");
            }

            Console.WriteLine(); // Add a separator between log files
        }


        // Output the results to the console

    }

    static async Task<Dictionary<string, int>> AnalyzeLogFileAsync(string filePath, string targetColumnName)
    {
        var ipHits = new Dictionary<string, int>();
        int targetColumnIndex = -1; // Initialize to an invalid index

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();

                    // Check if the line contains the header information
                    if (line.StartsWith("#Fields"))
                    {
                        // Extract column names from the header excluding the first element
                        string[] columnNames = line.Split().Skip(1).Where(x => !string.IsNullOrEmpty(x)).ToArray();

                        // Find the index of the target column dynamically
                        targetColumnIndex = Array.IndexOf(columnNames, targetColumnName);

                        if (targetColumnIndex != -1)
                        {
                            continue; // Move to the next line after processing the header
                        }

                    }

                    // Split the line into an array based on any whitespace characters
                    string[] columns = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                    // Check if the array has enough elements based on the target column index
                    if (targetColumnIndex != -1 && columns.Length > targetColumnIndex)
                    {
                        string targetColumnValue = columns[targetColumnIndex];

                        if (!string.IsNullOrEmpty(targetColumnValue) && IsValidIpAddress(targetColumnValue))
                        {
                            if (ipHits.ContainsKey(targetColumnValue))
                            {
                                ipHits[targetColumnValue]++;
                            }
                            else
                            {
                                ipHits[targetColumnValue] = 1;
                            }
                        }                       


                    }                    
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading the log file: {ex.Message}");
        }

        return ipHits;
    }

    static bool IsValidIpAddress(string ipAddressString)
    {
        // TryParse checks if the provided string is a valid IP address
        if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress))
        {
            // Check for specific IP address formats or restrictions if needed
            return true;
        }

        return false;
    }

    static async Task<string> ResolveHostNameAsync(string ipAddress)
    {
        if (hostnameCache.TryGetValue(ipAddress, out string cachedHostName))
        {
            return cachedHostName;
        }

        try
        {
            var getHostEntryTask = Dns.GetHostEntryAsync(ipAddress);
            var timeoutTask = Task.Delay(100); // 500 milliseconds timeout

            // Wait for either the DNS lookup task or the timeout task to complete
            var completedTask = await Task.WhenAny(getHostEntryTask, timeoutTask);

            if (completedTask == getHostEntryTask)
            {
                IPHostEntry hostEntry = await getHostEntryTask;
                string resolvedHostName = hostEntry.HostName;
                hostnameCache[ipAddress] = resolvedHostName; // Cache the result
                return resolvedHostName;
            }
            else
            {
                // DNS lookup timed out
                return "Unknown";
            }
        }
        catch (Exception)
        {
            // Handle other exceptions
            return "Unknown";
        }
    }

}

