using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.TextFiles
{
    public static class Help
    {
        public static void Show(BbsSession session, string topic)
        {
            string topicText = FindTopicText(topic);
            if (string.IsNullOrWhiteSpace(topicText))
                ShowCommands(session);
            else
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
                {
                    session.Io.OutputLine(topicText);
                }
            }
        }

        private static string FindTopicText(string topic)
        {
            if (!string.IsNullOrWhiteSpace(topic))
            {
                foreach (var cmd in _commands)
                {
                    if (topic.Equals(cmd.Key, StringComparison.CurrentCultureIgnoreCase))
                        return cmd.Value;
                    var parts = cmd.Key.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (true == parts?.Any(p => topic.Equals(p, StringComparison.CurrentCultureIgnoreCase)))
                        return cmd.Value;
                }
            }
            return null;
        }

        private static void ShowCommands(BbsSession session)
        {
            var builder = new StringBuilder();
            builder.AppendLine(" ** Text Files Commands Listing **");
            foreach (var cmd in _commands.Keys)
                builder.AppendLine(cmd);
            
            builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Yellow}{Constants.InlineColorizer}");
            builder.AppendLine("For detailed help on a command or subject type 'help (command)'.  For example for help on the 'dir' command type 'help dir'.");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                session.Io.OutputLine(builder.ToString());
            }
        }


        private static readonly IDictionary<string, string> _commands = new Dictionary<string, string>
        {
            {"?, help", _help},
            {"#", _directEntry},
            {"dir, ls, grep", _dir},
            {"cd, chdir", _cd},
            {"read, type, cat, more, less, nonstop, ns", _read},
            {"searching", _searching},
            {"quit, exit, /o", _exit},
            {"dnd", _dnd},
            {"chat", _chat},
            {"link", _link},
            {"users", _users},
            {"md, mkdir", _mkdir},
            {"edit, nano", _edit},
            {"del, rm", _del},
            {"deltree, rd, rmdir", _rd},
            {"publish, pub, unpublish, unpub", _pub},

        };

        private const string _help =
            "The '?' or 'help' command brings up this help system.  'help' (or '?') by itself will show the list of commands.  " +
            "For detailed help on a specific command or subject area type 'help (command/subject)'. " +
            "For example, for help on searching type 'help searching'";

        private const string _directEntry =
            "Although dos/*nix style commands like cd, dir, ls, type, cat etc... can be used on this system you can also change to directories " +
            "and read files simply by entering their names or numbers.  If you do this a description of the directory or file will be shown first " +
            "and then you will be asked if you want to either change to the directory or read the file.  " +
            "There are some edge cases where you will not be able to use the directory/file name and will need to use the number instead.  For " +
            "example, in the main (root) directory Jason Scott's top 100 is called '100' but if you try to enter it by typing '100' then the " +
            "system will look for a directory or file numbered (not named) 100.";

        private const string _dir =
            "The commands 'dir', 'ls', and 'grep' all list the sub-directories and files in the current directory.  There some differences between " +
            "these commands.  'dir' will list sub-directories and files with snippets of their descriptions.  'ls' will list only the names of " +
            "sub-directories and files and this is done in a \"wide\" format meaning more than one entry per line.  'grep' works exactly like 'dir' " +
            "except when it comes to searching.  Please read 'help searching' for information on how to use these commands to search for files.";

        private const string _cd =
            "Examples:cd 12, cd Jokes, chdir Humor, cd.., cd /, cd \\ \r\n\r\n" +
            "Changes to a directory.  Unlike dos/*nix you can't change multiple levels, for example you can't use 'cd humor/jokes' " +
            "instead you need to issue two separate 'cd' statements (one to get into 'humor' and another to get into 'jokes' which is " +
            "under 'humor'). cd.. - goes up one directory to the parent directory cd /, cd \\ - goes back to the root directory.  " +
            "Using 'cd' or 'chdir' will bypass the description of the directory and just change to it.";

        private const string _read =
            "Examples: read 13, read example.txt, type 27, nonstop example.txt\r\n\r\n" +
            "The 'read', 'type', 'cat', 'more', and 'less' shows you the contents of a text file.  By using one of these commands (as opposed to " +
            "just typing the filename or number) will bypass showing the description of the file and just output the content.  The 'nonstop' (or 'ns') " +
            "command is will also show the contents of the file but without 'more?' prompts.  " +
            "This is useful if you're writing the file to a buffer file or sending it directly to a printer and don't want the 'more?' " +
            "prompt to show up in the buffer or on the printout.";

        private const string _exit =
            "The 'exit', 'quit', and '/o' commands leave the text files browser and return to Community Chat.";

        private const string _dnd =
            "By default when you enter the text files browser you automatically go into Do Not Disturb mode.  In this mode you will not see " +
            "chat messages or other notifications (such as user logins/logouts).  However you can use the 'dnd' command to toggle whether " +
            "do not disturb mode is on or not.";

        private const string _chat =
            "Example: chat hello jimbob!\r\n\r\n" +
            "You can use the 'chat' command to send a chat message to the channel you're in without having to exit the text files browser.";

        private const string _link =
            "Examples: link 13, link example.txt\r\n\r\n" +
            "The 'link' command is similar to 'chat' in that it adds a chat message to the channel however you cannot enter the contents of the " +
            "message.  Instead the message is a link to a particular file.  Users in chat can then use /textread or /tr to read the text file " +
            "without having to go into the text files browser, navigate to the file, and call it up manually.";

        private const string _mkdir =
            "For use only in your own user file area.  Please see the help topic on Community Users area.\r\n\r\n" +
            "Examples: md MyStuff, mkdir MyReceipies\r\n\r\n" +
            "The 'md' or 'mkdir' commands will " +
            "create a new subdirectory within the current directory.  You need to be in your own Community Users area.";

        private const string _edit =
            "For use only in your own user file area.  Please see the help topic on Community Users area.\r\n\r\n" +
            "Examples: edit 13, edit example.txt\r\n\r\n" +
            "The 'edit' or 'nano' commands will enter a text editor to edit the contents of the specified file.  If the file does not exist " +
            "and you entered a name (and not a number) then a new file will be created.  Help about how to use the editor is available in " +
            "the editor by using the '/?' command.\r\n\r\n" +
            "When you save the file, if you leave the description blank the file will remain in an unpublished state.  This means that the file " +
            "will only be visible to you until you publish it.  " +
            "Please also see 'help publish' for information about publishing.";

        private const string _del =
            "For use only in your own user file area.  Please see the help topic on Community Users area.\r\n\r\n" +
            "Examples: del 13, del example.txt\r\n\r\n" +
            "The 'del' or 'rm' commands will delete a file if it is in your text file area.  If you just want to hide the file from other users " +
            "while you work on changing it then please refer to the 'unpublish' command instead.";

        private const string _rd =
            "For use only in your own user file area.Please see the help topic on Community Users area.\r\n\r\n" +
            "Examples: rd 11, rd MyDocs\r\n\r\n" +
            "The 'deltree', 'rd', and 'rmdir' commands remove one of your Community Users sub-directories and anything contained within it.  " +
            "This includes any files and other sub-directories.  Please use with caution.";

        private const string _pub =
            "For use only in your own user file area.Please see the help topic on Community Users area.\r\n\r\n" +
            "Examples: pub 11, pub example.txt, publish 11, unpublish example.txt\r\n\r\n" +
            "The 'publish' and 'pub' commands can be used to make unpublished files published.  This is where you can add a description of the file " +
            "and then the file will be listed for other users to read.\r\n\r\n" +
            "The 'unpublish' and 'unpub' commands can be used to make published files unpublished.  This deletes the description of the file " +
            "but retains the file contents.  You can then edit the contents, do any changes you want, and when you're ready for the file to be made " +
            "visible by other users then use the 'publish' command again.";

        private const string _users =
            "In addition to the Jason Scott text files archive the area under /CommunityUsers is reserved for users such as yourself " +
            "to create your own text files for the whole world to read!  To do this you will need a sub-directory with your username.  " +
            "For example if your username is JimBob then your text files area will be located in /CommunityUsers/JimBob.  To get this " +
            "sub-directory just send me (" + Constants.SysopName + ") an email requesting it.  You will then be able to use commands like " +
            "md, edit, and publish to create your own text files here on Mutiny Community!";

        private readonly static string _searching = string.Join("\r\n", new[]
        {
            "Searching: ",
            "You can search the current directory using 'dir', 'ls', and 'grep' by passing search terms to these commands.  ",
            "This will show you directories and/or files where their names, descriptions, or(in the case of files) content contain the keyword(s).",
            "DIR (search term(s)) - searches only filenames and descriptions ",
            "LS (search term(s)) - searches only filenames ",
            "GREP (search term(s)) - searches only file contents ",
            "",
            "All searches only search the current directory, subdirectories are not searched.",
            "You can use both OR and AND type searches and can combine them.  In general, keywords separated by spaces are treated as OR:  ",
            "'dir apple banana'  - will list directories and files that have either 'apple' OR 'banana' in their names or descriptions. ",
            "'ls apple banana'   - will list directories and files that have either 'apple' OR 'banana' in their names. ",
            "'grep apple banana' - will list files that have either 'apple' OR 'banana' in the file's content. ",
            "",
            "An ampersand (&) character can be used to perform AND type searches.You do this by separating keywords with the ",
            "'and' symbol (&) with no spaces between the words and the ampersand:",
            "'dir apple&banana' - will list directories and files that have both 'apple' AND 'banana' in their names or descriptions.",
            "",
            "You can also mix ORs and ANDs:",
            "'grep apple banana cherry&strawberry' - well list files where the contents contain:",
            "either 'apple' or 'banana' or BOTH 'cherry' and 'strawberry'",
            "",
            "Searching Within Texts: ",
            "While reading a document you can search for a keyword while at the pause (More?) prompt.",
            "To do this hit slash (/) and then type the keyword you want to find.  If a match is found after your current page then the text will move to a few lines before the match.  ",
            "To search for the next occurance of the keyword you can type a slash (/) and hit enter without typing the word.  If you do this the search is repeated with the same keyword you searched for previously."});
    }
}
