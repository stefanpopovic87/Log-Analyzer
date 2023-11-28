namespace LogAnalyzer
{
    public class LogItem
    {
        public string HostName { get; set; }
        public string Cip { get; set; }
        public int Counter { get; set; }

        public LogItem(string hostName, string cip, int counter)
        {
            HostName = hostName;
            Cip = cip;
            Counter = counter;
        }      
    }
}
