using System.Collections.Concurrent;
using System.Net;

namespace LogAnalyzer
{
    public static class LogAnalyzer
    {
        private static readonly ConcurrentDictionary<string, string> hostnameCache = new();

        public static async Task<IEnumerable<LogItem>> AnalyzeLogFileAsync(string file, string targetColumnName)
        {
            var items = new List<LogItem>();
            int targetColumnIndex = -1; // Initialize to an invalid index
            var semaphore = new SemaphoreSlim(1, 1); // Semaphore for synchronization

            try
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    var lines = await reader.ReadToEndAsync();
                    var linesArray = lines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var tasks = linesArray.Select(async line =>
                    {
                        if (line.StartsWith("#Fields"))
                        {
                            string[] columnNames = line.Split().Skip(1).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            targetColumnIndex = Array.IndexOf(columnNames, targetColumnName);
                            if (targetColumnIndex != -1)
                            {
                                return; // Move to the next line after processing the header
                            }
                        }

                        string[] columns = line.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

                        if (!line.StartsWith("#") && targetColumnIndex != -1 && columns.Length > targetColumnIndex)
                        {
                            string targetColumnValue = columns[targetColumnIndex];

                            if (!string.IsNullOrEmpty(targetColumnValue) && IsValidIpAddress(targetColumnValue))
                            {
                                var hostName = await ResolveHostNameAsync(targetColumnValue);

                                await semaphore.WaitAsync();
                                try
                                {
                                    var existingItem = items.FirstOrDefault(x => x.Cip == targetColumnValue);
                                    if (existingItem != null)
                                    {
                                        existingItem.Counter++;
                                    }
                                    else
                                    {
                                        var newItem = new LogItem(hostName, targetColumnValue, 1);
                                        items.Add(newItem);
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }
                        }
                    });

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the log file: {ex.Message}");
            }

            return items;
        }

        public static Dictionary<string, List<LogItem>> SortItems(Dictionary<string, IEnumerable<LogItem>> items)
        {
            return items.Select(kv => new
            {
                File = kv.Key,
                LogItems = kv.Value.OrderByDescending(item => item.Counter).ToList()
            }).ToDictionary(x => x.File, x => x.LogItems);
        }

        private static bool IsValidIpAddress(string ipAddressString)
        {
            // TryParse checks if the provided string is a valid IP address
            if (IPAddress.TryParse(ipAddressString, out IPAddress? ipAddress))
            {
                return true;
            }
            return false;
        }

        private static async Task<string> ResolveHostNameAsync(string ipAddress)
        {
            if (hostnameCache.TryGetValue(ipAddress, out string? cachedHostName))
            {
                // Retrieving from cache if IP address already exists
                return cachedHostName;
            }

            try
            {
                var getHostEntryTasks = Dns.GetHostEntryAsync(ipAddress);
                var timeoutTask = Task.Delay(100); // 100 milliseconds timeout

                // Wait for either the DNS lookup task or the timeout task to complete
                var completedTask = await Task.WhenAny(getHostEntryTasks, timeoutTask);

                if (completedTask == getHostEntryTasks)
                {
                    IPHostEntry hostEntry = await getHostEntryTasks;
                    string resolvedHostName = hostEntry.HostName;

                    // Cache the result
                    hostnameCache.TryAdd(ipAddress, resolvedHostName);
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
}
