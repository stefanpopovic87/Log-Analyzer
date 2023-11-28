namespace LogAnalyzer
{
    public static class FileReader
    {
        public static string[] GetFiles(string path, string searchPatter)
        {
            return Directory.GetFiles(path, searchPatter);
        }
    }
}
