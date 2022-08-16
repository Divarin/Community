using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Roll
    {
        private static readonly Random _random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));

        public static void Execute(BbsSession session, params string[] dices)
        {
            if (true != dices?.Any())
                dices = new[] { "1d6" };

            for (int d=0; d < dices?.Length; d++)
            {
                var dice = dices[d];
                if (string.IsNullOrWhiteSpace(dice)) return;
                dice = dice.ToLower();
                if (!dice.Contains('d')) return;
                if (dice.StartsWith("d")) dice = $"1{dice}";
                var parts = dice.Split(new[] { 'd' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return;
                if (int.TryParse(parts[0], out int numDice) &&
                    int.TryParse(parts[1], out int numSides) &&
                    numDice > 0 && numDice < 20 &&
                    numSides > 0 && numSides < 200)
                {
                    RollDice(session, numDice, numSides);
                }
            }
        }

        private static void RollDice(BbsSession session, int numDice, int numSides)
        {
            var builder = new StringBuilder();
            builder.Append($"{session.User.Name} rolls {numDice}d{numSides}: ");
            int sum = 0;
            for (int i = 0; i < numDice; i++)
            {
                int roll = _random.Next(1, numSides + 1);
                builder.Append($"{(i > 0 ? "+" : "")}{roll}");
                sum += roll;
            }
            if (numDice > 1)
                builder.Append($"={sum}");

            var msg = builder.ToString();
            session.Messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, msg));
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.OutputLine(msg);
        }
    }
}
