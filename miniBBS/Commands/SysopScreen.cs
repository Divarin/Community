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
        private static readonly int[] _colWidths = new[]
        {
            17, // username             
            17, // channel name         
            14, // session start        
            11,  // idle time in minutes
            20  // current location      
        };

        private static readonly IList<string> _logMessages = new List<string>();

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
            Console.Write($"Started at {DateTime.Now:yy-MM-dd HH:mm:ss} (loc) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"It is now  {DateTime.Now:yy-MM-dd HH:mm:ss} (loc) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║╚══════════════════════════════════════════════════════════════════════════════╝");

            Console.Write("Username".PadRight(_colWidths[0]));
            Console.Write("Channel".PadRight(_colWidths[1]));
            Console.Write("Start".PadRight(_colWidths[2]));
            Console.Write("Idle".PadRight(_colWidths[3]));
            Console.WriteLine("Location".PadRight(_colWidths[4]));
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

                Console.Write((session.User?.Name ?? string.Empty).PadRight(_colWidths[0]));
                Console.Write((session.Channel?.Name ?? string.Empty).PadRight(_colWidths[1]));
                Console.Write($"{session.SessionStartUtc:MM-dd HH:mm}".PadRight(_colWidths[2]));
                string idle = $"{Math.Min(99, session.IdleTime.Days)}d {session.IdleTime.Hours}h {session.IdleTime.Minutes}m";
                Console.Write(idle.PadRight(_colWidths[3]));
                Console.WriteLine(session.CurrentLocation.ToString().PadRight(_colWidths[4]));

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
