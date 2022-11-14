using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Menus
{
    public static class CommandList
    {
        public static readonly SortedSet<Cmd> _commands = new SortedSet<Cmd>(new CmdComparer())
        {
            new Cmd("/v",0), new Cmd("/ver",0), new Cmd("/version",0), new Cmd("/o",1), new Cmd("/off",1), 
            new Cmd("/g",1), new Cmd("/logoff",1), new Cmd("/cls",2), new Cmd("/clr",2), 
            new Cmd("/clear",2), new Cmd("/c",2), 
            new Cmd("/pass",3), new Cmd("/password",3), new Cmd("/pw",3), new Cmd("/pwd",3),
            new Cmd("/fauxmain", 4), new Cmd("/main", 4), new Cmd("/menu", 4), new Cmd("/fauxmenu", 4), 
            new Cmd("/fakemain", 4), new Cmd("/fakemenu", 4),
            new Cmd("/term",5), new Cmd("/setup",5), new Cmd("/emu",5), new Cmd("/bell",6), 
            new Cmd("/sound",6), new Cmd("/m", 7),
            new Cmd("/announce",8), new Cmd("/help",9), new Cmd("/?",9), 
            new Cmd("?",9), new Cmd("/??",9), new Cmd("/about",10), new Cmd("/a",10), new Cmd("/del",11), new Cmd("/delete",11), 
            new Cmd("/d",11), new Cmd("/typo",12), new Cmd("/edit",12), new Cmd("/s",12), new Cmd("/rere",13), new Cmd("/pin",14), 
            new Cmd("/pins",14), new Cmd("/pin", 14), new Cmd("/unpin", 14), new Cmd("/e",15), new Cmd("/end",15), new Cmd("/chl",16), 
            new Cmd("/chanlist",16), 
            new Cmd("/channellist",16), new Cmd("/channelist",16), new Cmd("/ch",17), new Cmd("/chan",17), new Cmd("/channel",17), 
            new Cmd("/w",18), new Cmd("/who",18), new Cmd("/u",19), new Cmd("/users",19), new Cmd("/f",20), new Cmd("/find",20), 
            new Cmd("/search",20), new Cmd("/fu",20), new Cmd("/fs",20), new Cmd("/afk",21), new Cmd("/read",22), new Cmd("/nonstop",22), 
            new Cmd("/ctx",23), new Cmd("/cx",23), new Cmd("/re",23), new Cmd("/ref",23), new Cmd("/wat",23), new Cmd("/ra", 23), new Cmd("/new",24), 
            new Cmd("/tz",25), new Cmd("/timezone",25), new Cmd("/time",25), new Cmd("/si",26), new Cmd("/session",26), 
            new Cmd("/sessioninfo",26), new Cmd("/ui",27), new Cmd("/user",27), new Cmd("/userinfo",27), new Cmd("/ci",28), new Cmd("/chat",28), 
            new Cmd("/chatinfo",28), new Cmd("/cal",29), new Cmd("/calendar",29), new Cmd("/index",30), new Cmd("/i",30), 
            new Cmd("/ipban",31), new Cmd("/pp",32), new Cmd("/keepalive",32), new Cmd("/ping",32), new Cmd("/newuser",33), 
            new Cmd("/wave",34), new Cmd("/poke",34), new Cmd("/smile",34), new Cmd("/frown",34), new Cmd("/wink",34), new Cmd("/nod",34), 
            new Cmd("/fairwell",35), new Cmd("/farewell",35), new Cmd("/goodbye",35), new Cmd("/bye",35), new Cmd("/me",35), 
            new Cmd("/online",35), new Cmd("/onl",35), new Cmd("/on",35),  new Cmd("/roll",36), 
            new Cmd("/random",36), new Cmd("/rnd",36), new Cmd("/dice",36), new Cmd("/die",36), new Cmd("/mail",37), new Cmd("/email",37), 
            new Cmd("/e-mail",37), new Cmd("/feedback",37), new Cmd("/texts",38), new Cmd("/textz",38), new Cmd("/text",38), new Cmd("/txt",38), 
            new Cmd("/file",38), new Cmd("/files",38), new Cmd("/filez",38), new Cmd("/myfiles",38), new Cmd("/textread",39), new Cmd("/tr",39), 
            new Cmd("/run",39), new Cmd("/exec",39), new Cmd(",",40), new Cmd("<",40), new Cmd(".",40), new Cmd(">",40), new Cmd("[",40), 
            new Cmd("]",40), new Cmd("{",40), new Cmd("}",40), new Cmd("/blurb", 41), new Cmd("/blurbadmin", 42),
            new Cmd("/hand", 43), new Cmd("/raise", 43), new Cmd("/raisehand", 43), new Cmd("/voice", 43), new Cmd("/m", 44),
            new Cmd("/vote", 45), new Cmd("/votes", 45), new Cmd("/poll", 45), new Cmd("/polls", 45),
            new Cmd("/game", 46), new Cmd("/games", 46),new Cmd("/prog", 46),new Cmd("/progs", 46),new Cmd("/door", 46),new Cmd("/doors", 46),
            new Cmd("/calc", 47), new Cmd("/calculate", 47), new Cmd("/calculator", 47), new Cmd("/whisper", 48), new Cmd("/wh", 48), 
            new Cmd("/r", 48), new Cmd("/reply", 48), new Cmd("/bots", 49), new Cmd("/dnd", 50), new Cmd("/uptime", 51), 
            new Cmd("/times", 52), new Cmd("/dates", 52), new Cmd("/date", 52), new Cmd("/when", 52), new Cmd("/ignore", 53), 
            new Cmd("/basic", 54), new Cmd("/bas", 54), new Cmd("/seen", 55)
        };

        public static void Execute(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
            {
                int lastGroupNum = -1;
                var builder = new StringBuilder();
                ConsoleColor clr = ConsoleColor.Gray;
                bool started = false;
                foreach (var cmd in _commands.GroupBy(x => x.GroupNum).SelectMany(x => x))
                {
                    if (cmd.GroupNum != lastGroupNum)
                    {
                        lastGroupNum = cmd.GroupNum;
                        clr = clr == ConsoleColor.DarkGreen ? ConsoleColor.Cyan : ConsoleColor.DarkGreen;
                    }
                    if (started)
                        builder.Append(", ");
                    else
                        started = true;

                    builder.Append($"{UserIoExtensions.WrapInColor(cmd.ToString(), clr)}");
                }
                session.Io.OutputLine(builder.ToString());
            }
        }

        public class Cmd
        {
            public Cmd(string commandText, int groupNum = 0)
            {
                CommandText = commandText;
                GroupNum = groupNum;
            }

            public string CommandText { get; private set; }
            public int GroupNum { get; private set; }

            public override string ToString()
            {
                return CommandText;
            }
        }

        public class CmdComparer : IComparer<Cmd>
        {
            public int Compare(Cmd x, Cmd y)
            {
                return x.CommandText.CompareTo(y.CommandText);
            }
        }
    }
}
