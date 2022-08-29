using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using System;

namespace miniBBS.Basic.Executors
{
    public static class Rnd
    {
        private static Random _random = new Random((int)DateTime.Now.Ticks);

        public static double Execute()
        {
            double r = _random.NextDouble();
            return r;
        }

        public static void SetSeed(string strSeed, Variables variables)
        {
            if (string.IsNullOrWhiteSpace(strSeed))
            {
                _random = new Random((int)DateTime.Now.Ticks);
            }
            else
            {
                strSeed = Evaluate.Execute(strSeed, variables);
                int seed;
                if (int.TryParse(strSeed, out seed))
                    _random = new Random(seed);
                else
                    throw new RuntimeException($"unable to parse '{strSeed}' as an integer");
            }
        }

    }
}
