using System;

namespace miniBBS.Basic.Executors
{
    public static class Instr
    {
        public static int Execute(string haystack, string needle)
        {
            int pos = haystack.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase);
            return pos+1;
        }
    }
}
