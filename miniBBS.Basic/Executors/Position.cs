using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Position
    {
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            int x, y;
            ParsePosition(line, variables, out x, out y);

            session.Io.SetPosition(x, y);
        }

        public static void Right(BbsSession session, string args, Variables variables)
        {
            int count = ParseCount(args, variables);
            string str = session.Io.Right.Repeat(count);
            var bytes = str.Select(b => (byte)b).ToArray();
            session.Io.OutputRaw(bytes);
        }

        public static void Left(BbsSession session, string args, Variables variables)
        {
            int count = ParseCount(args, variables);
            string str = session.Io.Left.Repeat(count);
            var bytes = str.Select(b => (byte)b).ToArray();
            session.Io.OutputRaw(bytes);
        }

        public static void Down(BbsSession session, string args, Variables variables)
        {
            int count = ParseCount(args, variables);
            string str = session.Io.Down.Repeat(count);
            var bytes = str.Select(b => (byte)b).ToArray();
            session.Io.OutputRaw(bytes);
        }

        public static void Up(BbsSession session, string args, Variables variables)
        {
            var count = ParseCount(args, variables);
            var str = session.Io.Up.Repeat(count);
            var bytes = str.Select(b => (byte)b).ToArray();
            session.Io.OutputRaw(bytes);
        }

        public static void Home(BbsSession session)
        {
            var str = session.Io.Home;
            var bytes = str.Select(b => (byte)b).ToArray();
            session.Io.OutputRaw(bytes);
        }

        private static void ParsePosition(string line, Variables variables, out int x, out int y)
        {
            if (string.IsNullOrWhiteSpace(line))
                throw new RuntimeException("POSITION without X,Y coordinates");

            string[] parts = line
                .Split(',')
                .Select(p => Evaluate.Execute(p, variables))
                .ToArray();

            if (parts.Length != 2)
                throw new RuntimeException("POSITION with invalid X,Y coordinates");

            if (!int.TryParse(parts[0], out x) || x < 1 || x > 80)
                throw new RuntimeException("POSITION with invalid X coordinate");

            if (!int.TryParse(parts[1], out y) || y < 1 || y > 25)
                throw new RuntimeException("POSITION with invalid Y coordinate");
        }

        private static int ParseCount(string args, Variables variables)
        {
            int count = 1;
            if (!string.IsNullOrWhiteSpace(args))
            {
                args = Evaluate.Execute(args, variables);
                if (!int.TryParse(args, out count))
                    throw new RuntimeException("type mismatch");
            }
            return count;
        }

        
    }
}
