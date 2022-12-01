using miniBBS.Core;
using miniBBS.Extensions_Exception;
using miniBBS.TextFiles.Models;
using System;
using System.IO;

namespace miniBBS.TextFiles
{
    public static class FileReader
    {
        public static string ReadFile(FileInfo file)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string contents = reader.ReadToEnd();
                return contents;
            }
        }

        public static string LoadFileContents(Link currentLocation, Link link)
        {
            string path = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{link.Path}";
            try
            {
                var fi = new FileInfo(path);
                var body = ReadFile(fi);
                return body;
            }
            catch (Exception ex)
            {
                ex = new Exception($"File Path: {path}  {ex.InnermostException().Message}");
                throw ex;
            }
        }
    }
}
