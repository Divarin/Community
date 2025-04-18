﻿using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace miniBBS.Services.GlobalCommands
{
    public static class SysopScreen
    {
        private static ISessionsList _sessionsList;
        /// <summary>
        /// [IP address] = count of connections from that IP (not including gopher server)
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _ips = new ConcurrentDictionary<string, int>();
        /// <summary>
        /// [IP address] = count of connections (to the Gopher Server) from that IP
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _gopherIps = new ConcurrentDictionary<string, int>();
        /// <summary>
        /// [Gopher Selector] = count of requests for the given gopher document
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _gopherSelectors = new ConcurrentDictionary<string, int>();
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
        public static DateTime StartedAtUtc { get; private set; } = DateTime.UtcNow;
        private static readonly List<LoginRecord> _logins = new List<LoginRecord>();

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

        public static void BeginLogin(BbsSession session)
        {
            _logins.Add(new LoginRecord
            {
                SessionId = session.Id,
                IpAddress = session.IpAddress,
                Username = session.User?.Name,
                LoginAtLocal = session.SessionStartUtc.ToLocalTime()
            });
        }

        public static void EndLogin(BbsSession session)
        {
            if (session == null)
                return;

            var record = _logins.FirstOrDefault(l => l.SessionId.Equals(session.Id));
            if (record != null)
                record.LogoutAtLocal = DateTime.Now;
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
            Console.Write($"{Constants.BbsName} Version {Constants.Version}".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"Started at {_startedAtLoc:yy-MM-dd HH:mm:ss} (local) [{StartedAtUtc:HH:mm} (utc)]".PadAndCenter(78));
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("║║");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"It is now  {DateTime.Now:yy-MM-dd HH:mm:ss} (local) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
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

        public static void SetLastConnectionIp(string ip)
        {
            if (!_ips.ContainsKey(ip))
                _ips[ip] = 1;
            else
                _ips[ip]++;
        }

        public static void RegisterGopherServerRequest(string ip, string selector)
        {
            if (!_gopherIps.ContainsKey(ip))
                _gopherIps[ip] = 1;
            else
                _gopherIps[ip]++;

            if (!_gopherSelectors.ContainsKey(selector))
                _gopherSelectors[selector] = 1;
            else
                _gopherSelectors[selector]++;
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
            Console.Write($"It is now  {DateTime.Now:yy-MM-dd HH:mm:ss} (local) [{DateTime.UtcNow:HH:mm} (utc)]".PadAndCenter(78));
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
                if (i % 2 == 0)
                    Console.ForegroundColor = ConsoleColor.White;
                else
                    Console.ForegroundColor = ConsoleColor.Gray;

                var flags = $"{(session.DoNotDisturb ? "D" : " ")}{(session.Afk ? "A" : " ")}";
                Console.Write(flags.PadRight(_colWidths[(int)Col.Flags]));
                Console.Write((session.User?.Name ?? session.IpAddress ?? "???").PadRight(_colWidths[(int)Col.Username]));
                Console.Write((session.Channel?.Name ?? string.Empty).PadRight(_colWidths[(int)Col.ChannelName]));
                Console.Write($"{session.SessionStartUtc.ToLocalTime():MM-dd HH:mm}".PadRight(_colWidths[(int)Col.SessionStart]));
                string idle = $"{Math.Min(99, session.IdleTime.Days)}d {session.IdleTime.Hours}h {session.IdleTime.Minutes}m";
                Console.Write(idle.PadRight(_colWidths[(int)Col.IdleTime]));
                Console.WriteLine(session.CurrentLocation.ToString().PadRight(_colWidths[(int)Col.CurrentLocation]));

                i++;
            }

            for (int j = i; j <= _numSessionsToList; j++)
            {
                Console.Write(_blankLine);
            }

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            Console.Write($" (I)ssues: {_logMessages.Count} | (C)lear Issues | (L)ogins: {_logins.Count} ({_logins.Select(l => l.Username).Distinct().Count()} users) | I(P)s | (G)opher Stats ");
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.I:
                        DisplayLogs();
                        RedrawFromStart();
                        break;
                    case ConsoleKey.C:
                        _logMessages.Clear();
                        break;
                    case ConsoleKey.L:
                        DisplayLogins();
                        RedrawFromStart();
                        break;
                    case ConsoleKey.P:
                        ShowTopIpConnections();
                        RedrawFromStart();
                        break;
                    case ConsoleKey.G:
                        ShowGopherStats();
                        RedrawFromStart();
                        break;
                }
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ShowTopIpConnections()
        {
            var ips = _ips
                .OrderByDescending(x => x.Value)
                .Take(15)
                .Select(x => $"{x.Key} ({x.Value})")
                .ToList();

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            foreach (var ip in ips)
                Console.WriteLine(ip);

            Console.WriteLine("press any key");
            Console.ReadKey();
        }

        private static void ShowGopherStats()
        {
            var uniqueVisitors = _gopherIps.Keys.Count();
            var totalHits = _gopherIps.Sum(x => x.Value);
            var top5Ips = _gopherIps
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToArray();
            var top5Selectors = _gopherSelectors
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToArray();

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            Console.WriteLine($"Total hits: {totalHits}");
            Console.WriteLine($"Unique IPs: {uniqueVisitors}");
            Console.WriteLine("Top 5 IPs:");
            foreach (var s in top5Ips)
                Console.WriteLine($"{s.Value} : {s.Key}");
            Console.WriteLine("Top 5 selectors:");
            foreach (var s in top5Selectors)
                Console.WriteLine($"{s.Value} : {s.Key}");

            Console.WriteLine("press any key");
            Console.ReadKey();
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

        private static void DisplayLogins()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            foreach (var login in _logins)
                Console.WriteLine(login);
            Console.WriteLine("press any key");
            Console.ReadKey();
        }
    }
}
