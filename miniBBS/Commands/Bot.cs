using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Bot
    {
        public static void Execute(BbsSession session, string scriptName, string scriptInput)
        {
            var browser = DI.Get<ITextFilesBrowser>();
            var scripts = browser.FindBasicPrograms(session, scripts: true).ToList();
            var bots = scripts.Where(x => x.EndsWith($"/{scriptName}.mbs", StringComparison.CurrentCultureIgnoreCase)).ToList();
            string bot = null;

            if (bots.Count < 1)
            {
                session.Io.Error($"No bot named '{scriptName}'");
                return;
            }
            else if (bots.Count > 1)
            {
                session.Io.Error($"There are {bots.Count} bots named '{scriptName}'.");
                bot = SelectBot(session, bots);
            }
            else
                bot = bots.First();

            if (string.IsNullOrWhiteSpace(bot))
                return;

            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, 
                $"<{session.User.Name} to {scriptName.ToUpper()}BOT> {scriptInput}"));
            browser.RunScript(session, bot, scriptInput);
        }

        public static void ListBots(BbsSession session, string searchTerm = null)
        {
            var browser = DI.Get<ITextFilesBrowser>();
            var scripts = browser.FindBasicPrograms(session, scripts: true).ToList();
            var bots = scripts.Where(x => x.EndsWith($".mbs", StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                bots = bots.Where(x => x.ToUpper().Contains(searchTerm.ToUpper())).ToList();
            var builder = new StringBuilder();
            
            builder.AppendLine(" *** Bots ***");
            foreach (var bot in bots)
                builder.AppendLine(bot);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.Output(builder.ToString());
        }

        private static string SelectBot(BbsSession session, List<string> bots)
        {
            var builder = new StringBuilder();
            for (int i=0; i < bots.Count; i++)
                builder.AppendLine($"{i+1} : {bots[i]}");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.Output(builder.ToString());
                session.Io.Output("Select BOT # or 0 to quit: ");
                var k = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(k) && int.TryParse(k, out int n) && n >= 1 && n <= bots.Count)
                    return bots[n - 1];
            }

            return null;
        }
    }
}
