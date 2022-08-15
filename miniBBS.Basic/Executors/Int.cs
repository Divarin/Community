namespace miniBBS.Basic.Executors
{
    public static class Int
    {
        public static int Execute(string n)
        {
            double d;
            if (double.TryParse(n, out d))
                return Execute(d);
            else
                return 0;
        }
        public static int Execute(double n)
        {
            return (int)n;
        }
    }
}
