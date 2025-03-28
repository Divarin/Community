using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class More
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr("More Prompt Help", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            "When reading a large block of text the system will provide pauses each page-full.",
            "The page size is determined by your terminal 'rows' setting.",
            $"This is something you set during login and can be changed from the main menu using {Constants.Inverser}{"P".Color(ConsoleColor.Green)}{Constants.Inverser})refs, or from chat using '{Constants.Inverser}{"/term".Color(ConsoleColor.Green)}{Constants.Inverser}'",
            "This 'More' prompt provides access to some unique features, explained below.",
            $"{Constants.Spaceholder}",
            $"{_clr($"{Constants.Inverser}Y{Constants.Inverser}", ConsoleColor.Green)} : Yes, continue to next page (also {Constants.Inverser}{"ENTER".Color(ConsoleColor.Green)}{Constants.Inverser}).",
            $"{_clr($"{Constants.Inverser}N{Constants.Inverser}", ConsoleColor.Green)} : No, aborts reading.",
            $"{_clr($"{Constants.Inverser}C{Constants.Inverser}", ConsoleColor.Green)} : Continuous, continues reading and does not pause anymore..",
            $"{_clr($"{Constants.Inverser}U{Constants.Inverser}", ConsoleColor.Green)} : Page Up, jumps backward one page.",
            $"{_clr($"{Constants.Inverser}S{Constants.Inverser}", ConsoleColor.Green)} : Save for later, saves the text for you read it later.  On your next login you'll be asked if you want to continue reading, also from chat you can do so using the '{Constants.Inverser}{"/bookmark".Color(ConsoleColor.Green)}{Constants.Inverser}' command.",
            $"{_clr($"{Constants.Inverser}/{Constants.Inverser}", ConsoleColor.Green)} : Start or continue a keyword search.  To start type keyword(s), to search for the next occurance of the previous keyword just hit '{"ENTER".Color(ConsoleColor.Green)}' when asked for the search term.",
            $"{_clr($"{Constants.Inverser}0{Constants.Inverser}", ConsoleColor.Green)} : Jumps to start of message.",
            $"{_clr($"{Constants.Inverser}1-9{Constants.Inverser}", ConsoleColor.Green)} : Jumps to approx. 10% to 90% into the message.",
            $"{_clr($"{Constants.Inverser}?{Constants.Inverser}", ConsoleColor.Green)} : At the 'more' prompt you can hit '?' to bring up a quick summary of these commands.",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
