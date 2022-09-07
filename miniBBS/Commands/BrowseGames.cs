using miniBBS.Core;
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
            var builder = new StringBuilder();

            builder.AppendLine(UserIoExtensions.WrapInColor("*** User Programs Listing ***", ConsoleColor.Yellow));
            builder.Append(UserIoExtensions.WrapInColor("To run a program, type '", ConsoleColor.White));
            builder.Append(UserIoExtensions.WrapInColor("/run #", ConsoleColor.Green));
            builder.AppendLine(UserIoExtensions.WrapInColor("' where # is the program number.", ConsoleColor.White));

            for (int i=0; i < gamesList.Count; i++)
            {
                var parts = gamesList[i].Split('|');
                var path = parts[0];
                var desc = parts.Length > 1 ? parts[1] : string.Empty;
                builder.AppendLine($"{(i + 1).ToString().PadLeft(3, Constants.Spaceholder)} : {UserIoExtensions.WrapInColor(path, ConsoleColor.DarkGray)}");
                builder.AppendLine(UserIoExtensions.WrapInColor(desc, ConsoleColor.Blue));
            }
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                session.Io.Output(builder.ToString());
            }
        }
    }
}
