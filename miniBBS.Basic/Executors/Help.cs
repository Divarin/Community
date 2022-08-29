using miniBBS.Basic.Interfaces;
using miniBBS.Core.Models.Control;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Help
    {
        public static void Display(BbsSession session, string topic=null)
        {
            if (string.IsNullOrWhiteSpace(topic))
                DisplayOverview(session);
            else
            {
                var topicHelp = GetTopicHelp(topic)?.ToList();
                if (true != topicHelp?.Any())
                    session.Io.OutputLine($"? no help available on the topic '{topic}'");
                else
                {
                    for (int i=0; i < topicHelp.Count; i++)
                    {
                        if (i>0 && i%20 == 0)
                        {
                            session.Io.Output("[Pause]");
                            session.Io.InputKey();
                            session.Io.OutputLine();
                        }
                        session.Io.OutputLine(topicHelp[i]);
                    }
                }
            }       
        }

        private static void DisplayOverview(BbsSession session)
        {
            //                     12345678901234567890123456789012345678901234567890123456789012345678901234567890
            session.Io.OutputLine("Help Overview");
            session.Io.OutputLine("help [topic] for help on a particular command or topic.");
            session.Io.OutputLine("example: \"help print\" shows help on the \"print\" command.");
            session.Io.OutputLine();
            session.Io.OutputLine("Most important topics for newbies: ");
            session.Io.OutputLine("* = not yet implemented");
            session.Io.OutputLine("QUIT - exit Mutant Basic (doesn't save anything)");
            session.Io.OutputLine("ESC  - if pressed while your program is running breaks the program (stops it)");
            session.Io.OutputLine("LET - sets a variable value: LET X=22/7  or LET FOO=\"BAR\"");
            session.Io.OutputLine("VARS - Lists all variables and their values");
            session.Io.OutputLine("NEW - deletes your current program from memory (saved programs are safe)");
            session.Io.OutputLine("RUN - runs your program (the one in memory)");
            session.Io.OutputLine();
            session.Io.OutputLine("Filesystem commands:");
            session.Io.OutputLine("DISKS - Lists all of your disks");
            session.Io.OutputLine("NEWDISK \"diskname\" - Creates a new disk with the given name");
            session.Io.OutputLine("DELDISK \"diskname\" - Deletes a disk with the given name");
            session.Io.OutputLine("DISK \"diskname\" - Sets the current disk to the one with the given name");
            session.Io.OutputLine("DIR - Lists basic programs on the disk");
            session.Io.OutputLine("LOAD \"filename\" - Loads basic program from disk to memory");
            session.Io.OutputLine("SAVE \"filename\" - Saves basic program from memory to disk");
            session.Io.OutputLine("DEL \"filename\" - Deletes basic program from disk");
            session.Io.OutputLine("DIR - Lists contents of the virtual disk");
        }

        private static IEnumerable<string> GetTopicHelp(string topic)
        {
            switch (topic.ToLower())
            {
                case "print":
                    //            12345678901234567890123456789012345678901234567890123456789012345678901234567890
                    yield return "Displays a line of text on the screen.";
                    yield return "Can be used for string literals (in quotes) as well as evaluated expressions";
                    yield return "Examples:";
                    yield return "PRINT \"HELLO\"";
                    yield return "prints: HELLO";
                    yield return "PRINT 5*7";
                    yield return "prints: 35";
                    yield return "PRINT \"HELLO\"5*7";
                    yield return "prints: HELLO35";
                    break;
            }
            yield break;
        }
    }
}
