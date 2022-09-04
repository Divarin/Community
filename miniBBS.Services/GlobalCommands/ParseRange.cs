using System;

namespace miniBBS.Services.GlobalCommands
{
    public static class ParseRange
    {
        public static Tuple<int, int> Execute(string range, int upperLimit)
        {
            if (string.IsNullOrWhiteSpace(range))
                return MakeTuple(1, upperLimit, upperLimit);
            if (int.TryParse(range, out int n))
            {
                if (n < 0)
                    return MakeTuple(1, -n, upperLimit);
                else if (n == 0)
                    return MakeTuple(1, 1, upperLimit);
                else
                    return MakeTuple(n, n, upperLimit);
            }
            var parts = range.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && int.TryParse(parts[0], out int a))
                return MakeTuple(a, upperLimit, upperLimit);
            if (parts.Length > 1 && int.TryParse(parts[0], out int a1) && int.TryParse(parts[1], out int b1))
                return MakeTuple(a1, b1, upperLimit);
            return MakeTuple(1, upperLimit, upperLimit);
        }

        private static Tuple<int, int> MakeTuple(int a, int b, int upperLimit)
        {
            if (a < 1) a = 1;
            if (a > upperLimit) a = upperLimit;
            if (b < 1) b = 1;
            if (b > upperLimit) b = upperLimit;
            a = Math.Min(a, b);
            b = Math.Max(a, b);
            return new Tuple<int, int>(a, b);
        }
    }
}
