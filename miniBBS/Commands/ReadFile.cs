using miniBBS.Core.Models.Control;
using System.IO;

namespace miniBBS.Commands
{
    public static class ReadFile
    {
        private static readonly object _lock = new object();

        public static void Execute(BbsSession session, string filename)
        {
            string fileContents = GetFileContents(filename);
            session.Io.OutputLine(fileContents);
        }

        private static string GetFileContents(string filename)
        {
            lock (_lock)
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(fs))
                {
                    string contents = reader.ReadToEnd();
                    return contents;
                }
            }
        }
    }
}
