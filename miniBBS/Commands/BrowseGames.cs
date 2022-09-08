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

            builder.AppendLine("*** User Programs Listing ***".Color(ConsoleColor.Yellow));
            builder.Append("To run a program, type '".Color(ConsoleColor.White));
            builder.Append("/run #".Color(ConsoleColor.Green));
            builder.AppendLine("' where # is the program number.".Color(ConsoleColor.White));

            for (int i=0; i < gamesList.Count; i++)
            {
                var parts = gamesList[i].Split('|');
                var path = parts[0];
                var desc = parts.Length > 1 ? parts[1] : string.Empty;
                builder.AppendLine($"{(i + 1).ToString().PadLeft(3, Constants.Spaceholder)} : {UserIoExtensions.WrapInColor(path, ConsoleColor.DarkGray)}");
                builder.AppendLine(desc.Color(ConsoleColor.Blue));
            }

            builder.AppendLine("Want to write your own programs in Basic?  Use '/feedback' to ask the Sysop about it to get started!".Color(ConsoleColor.White));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                session.Io.Output(builder.ToString());
            }
        }
    }
}
