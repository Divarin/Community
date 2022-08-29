using System;

namespace miniBBS.Basic.Executors
{
    public static class Sqr
    {
        public static double Execute(string n)
        {
            return Execute(double.Parse(n));
        }

        public static double Execute(int n)
        {
            return Execute((double)n);
        }

        public static double Execute(double n)
        {
            return Math.Sqrt(n);
        }
    }
}
