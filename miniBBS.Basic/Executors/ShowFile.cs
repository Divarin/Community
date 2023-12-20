using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System.IO;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class ShowFile
    {
        public static void Execute(BbsSession session, string rootDir, string filename, Variables variables)
        {
            filename = Evaluate.Execute(filename, variables);
            filename = new string(filename.Where(c =>
                char.IsLetterOrDigit(c)
                || c == '.'
                || c == '_'
                || c == '-').ToArray());
            
            if (!File.Exists(rootDir + filename))
            {
                session.Io.Error($"Unable to find file '{filename}'.");
                return;
            }

            filename = rootDir + filename;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                session.Io.Output(contents);
            }
        }
    }
}
