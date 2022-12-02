using miniBBS.Core.Interfaces;
using miniBBS.Extensions;
using miniBBS.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class ProgramData
    {
        public static SortedList<int, string> Deserialize(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new SortedList<int, string>();

            var json = GlobalDependencyResolver.Default.Get<ICompressor>().Decompress(data);
            SortedList<int, string> result = JsonConvert.DeserializeObject<SortedList<int, string>>(json);
            return result;
        }

        public static string Serialize(SortedList<int, string> lines)
        {
            if (true != lines?.Any())
                return string.Empty;

            string json = JsonConvert.SerializeObject(lines);
            string compressed = GlobalDependencyResolver.Default.Get<ICompressor>().Compress(json);
            return compressed;
        }
    }
}
