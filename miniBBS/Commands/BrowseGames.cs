using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class BrowseGames
    {
        public static void Execute(BbsSession session)
        {
            var browser = DI.Get<ITextFilesBrowser>();
            var gamesList = browser.FindBasicPrograms(session).ToList();
            if (gamesList == null || gamesList.Count < 1)
            {
                session.Io.Error("No games.");
                return;
            }

            var builder = new StringBuilder();

            builder.AppendLine("*** User Programs Listing ***".Color(ConsoleColor.Yellow));
            if (session.CurrentLocation == Module.Chat)
            {
                builder.Append("To run a program, type '".Color(ConsoleColor.White));
                builder.Append("/run #".Color(ConsoleColor.Green));
                builder.AppendLine("' where # is the program number.".Color(ConsoleColor.White));
            }

            for (int i=0; i < gamesList.Count; i++)
            {
                var parts = gamesList[i].Split('|');
                var desc = parts.Length > 1 ? parts[1] : string.Empty;
                var owner = GetOwner(parts[0]);
                builder.AppendLine($"{(i + 1).ToString().PadLeft(3, Constants.Spaceholder)} : {desc.Color(ConsoleColor.Blue)} by {owner}");
            }

            builder.AppendLine("Want to write your own programs in Basic?  Use '/feedback' to ask the Sysop about it to get started!".Color(ConsoleColor.White));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                session.Io.Output(builder.ToString());
            }

            if (session.CurrentLocation == Module.MainMenu)
            {
                session.Io.Output($"{Constants.Inverser}Enter program # :{Constants.Inverser} ".Color(ConsoleColor.White));
                var inp = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(inp) && int.TryParse(inp, out int n) && n >= 1 && n <= gamesList.Count)
                {
                    ReadTextFile.Execute(session, inp);
                }
            }
        }

        private static string GetOwner(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var parts = path.Split('/');
            if (parts != null && parts.Length >= 2 && parts[0].Equals("users", StringComparison.CurrentCultureIgnoreCase))
                return parts[1];
            return null;
        }
    }
}
