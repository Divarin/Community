using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace miniBBS.Commands
{
    public static class SysopScreen
    {
        private static ISessionsList _sessionsList;
        const int _numSessionsToList = 16;
        private static readonly string _blankLine = ' '.Repeat(80);
        private enum Col
        {
            Flags,
            Username,
            ChannelName,
            SessionStart,
            IdleTime,
            CurrentLocation
        };

        private static readonly int[] _colWidths = new[]
        {
            3, // flags
            16, // username             
            16, // channel name         
            14, // session start        
            11,  // idle time in minutes
            19  // current location      
        };

        private static readonly IList<string> _logMessages = new List<string>();
        private static readonly DateTime _startedAtLoc = DateTime.Now;
        private static readonly DateTime _startedAtUtc = DateTime.UtcNow;
        public static void Initialize(ISessionsList sessionsList)
        {
            _sessionsList = sessionsList;

            RedrawFromStart();

            ThreadStart start = new ThreadStart(DrawLoop);
            Thread thread = new Thread(start);
            thread.Start();
        }

        public static void AddLogMessage(string message)
        {
            _logMessages.Add(message);
        }

        private static void RedrawFromStart()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Write("╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.Write("║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"Mutiny Community Version {Constants.Version}".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"Started at {_startedAtLoc:yy-MM-dd HH:mm:ss} (loc) [{_startedAtUtc:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"It is now  {DateTime.Now:yy-MM-dd HH:mm:ss} (loc) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║╚══════════════════════════════════════════════════════════════════════════════╝");

            Console.Write("DA".PadRight(_colWidths[(int)Col.Flags]));
            Console.Write("Username".PadRight(_colWidths[(int)Col.Username]));
            Console.Write("Channel".PadRight(_colWidths[(int)Col.ChannelName]));
            Console.Write("Start".PadRight(_colWidths[(int)Col.SessionStart]));
            Console.Write("Idle".PadRight(_colWidths[(int)Col.IdleTime]));
            Console.WriteLine("Location".PadRight(_colWidths[(int)Col.CurrentLocation]));
        }

        private static void DrawLoop()
        {
            while (true)
            {
                Draw();
                Thread.Sleep(1000);
            }
        }

        private static void Draw()
        {
            Console.SetCursorPosition(0, 3);
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"It is now  {DateTime.Now:yy-MM-dd HH:mm:ss} (loc) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║");

            Console.SetCursorPosition(0, 6);
            Console.BackgroundColor = ConsoleColor.Black;
            
            int i = 0;
            foreach (var session in _sessionsList.Sessions)
            {
                if (i > _numSessionsToList)
                    break;
                if (i%2==0)
                    Console.ForegroundColor = ConsoleColor.White;
                else
                    Console.ForegroundColor = ConsoleColor.Gray;

                var flags = $"{(session.DoNotDisturb ? "D" : " ")}{(session.Afk ? "A" : " ")}";
                Console.Write(flags.PadRight(_colWidths[(int)Col.Flags]));
                Console.Write((session.User?.Name ?? string.Empty).PadRight(_colWidths[(int)Col.Username]));
                Console.Write((session.Channel?.Name ?? string.Empty).PadRight(_colWidths[(int)Col.ChannelName]));
                Console.Write($"{session.SessionStartUtc:MM-dd HH:mm}".PadRight(_colWidths[(int)Col.SessionStart]));
                string idle = $"{Math.Min(99, session.IdleTime.Days)}d {session.IdleTime.Hours}h {session.IdleTime.Minutes}m";
                Console.Write(idle.PadRight(_colWidths[(int)Col.IdleTime]));
                Console.WriteLine(session.CurrentLocation.ToString().PadRight(_colWidths[(int)Col.CurrentLocation]));

                i++;
            }

            for (int j=i; j <= _numSessionsToList; j++)
            {
                Console.Write(_blankLine);
            }

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" (L)og entries: {_logMessages.Count} ");
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.L)
                {
                    DisplayLogs();
                    RedrawFromStart();
                }
                else if (key.Key == ConsoleKey.C)
                    _logMessages.Clear();
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void DisplayLogs()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            foreach (var log in _logMessages)
                Console.WriteLine(log);
            Console.WriteLine("press any key");
            Console.ReadKey();
        }
    }
}
