using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Subscribers;
using miniBBS.UserIo;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace miniBBS.Commands
{
    public static class NullSpace
    {
        private static readonly ConcurrentDictionary<ConsoleColor, bool> _keys = new ConcurrentDictionary<ConsoleColor, bool>();
        private static readonly object _lock = new object();
        private static readonly ConsoleColor[] _keyColors = new[]
        {
            ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Cyan,
            ConsoleColor.Blue, ConsoleColor.Magenta, ConsoleColor.Red
        };
        private static readonly TimeSpan _maxIdleTime = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan _warnIdleTime = _maxIdleTime.Subtract(TimeSpan.FromMinutes(2));

        static NullSpace()
        {
            foreach (var color in _keyColors)
                _keys.TryAdd(color, true);
        }

        public static void Enter(BbsSession session)
        {
            var ghosts = DI.Get<ISessionsList>()
                .Sessions
                .Where(x => x.User.Id == session.User.Id && x.CurrentLocation == Core.Enums.Module.NullSpace)
                .ToList();

            foreach (var ghost in ghosts)
            {
                ghost.ForceLogout = true;
                session.Io.OutputLine("Your ghost session has 'gone into the light', so to speak.");
            }

            ConsoleColor keyColor;
            lock (_lock)
            {
                var clr = GetKey(session);
                if (!clr.HasValue)
                    return;
                keyColor = clr.Value;
                _keys[keyColor] = false;
            }

            var messenger = DI.Get<IMessager>();
            var originalDnd = session.DoNotDisturb;
            var originalArea = session.CurrentLocation;
            var originalPrompt = session.ShowPrompt;

            session.CurrentLocation = Core.Enums.Module.NullSpace;
            session.DoNotDisturb = true;
            session.ShowPrompt = () => { };

            session.Io.OutputLine($"{session.Io.NewLine}You have entered a strange and dark place.  Use ESC or CTRL+C to leave!");

            var subscriber = new NullSpaceSubscriber(session);
            
            try
            {
                messenger.Subscribe(subscriber);

                var enteredMessage = "\r\nYou feel a new presence\r\n";
                messenger.Publish(session, new NullSpaceMessage(session, keyColor, enteredMessage));

                session.Io.PollKey();
                var lastKeyPoll = session.Io.GetPolledTicks();
                var exit = false;
                session.Io.SetForeground(keyColor);

                while (!exit)
                {
                    var idleTimeoutWarned = false;
                    while (true)
                    {
                        var polledTicks = session.Io.GetPolledTicks();
                        var idleTime = session.IdleTime;
                        if (session.ForceLogout || idleTime >= _maxIdleTime || !session.IsConnected)
                        {
                            exit = true;
                            break;
                        }
                        if (!idleTimeoutWarned && idleTime.TotalMinutes == _warnIdleTime.TotalMinutes)
                        {
                            session.Io.OutputLine("You feel yourself being pulled out of NullSpace (you're approach idle-timeout)");
                            idleTimeoutWarned = true;
                        }

                        if (lastKeyPoll < polledTicks)
                            break;
                        
                        Thread.Sleep(25);
                    }
                    var key = session.Io.GetPolledKey();
                    lastKeyPoll = session.Io.GetPolledTicks();
                    if (!key.HasValue)
                        continue;
                    
                    idleTimeoutWarned = false;
                    var isEscOrCtrlC =
                        key == 3 ||
                        key == 27 ||
                        (session.Io is Atascii && (key == 30 || key == 0));

                    if (isEscOrCtrlC)
                        exit = true;

                    if (exit)
                        continue;

                    var msg = $"{key}";
                    var isNewline =
                        msg == "\r" ||
                        (session.Io is Atascii && key == 155);
                    var isBackspace =
                        msg == "\b" ||
                        key == 127 ||
                        (session.Io is Cbm && key == 20);

                    if (isNewline)
                        msg = Environment.NewLine;
                    else if (isBackspace)
                        msg = "\b \b";

                    messenger.Publish(session, new NullSpaceMessage(session, keyColor, msg));
                    session.Io.Output(msg);
                }
            }
            finally
            {
                session.Io.AbortPollKey();

                lock (_lock)
                {
                    _keys[keyColor] = true;
                }

                var departedMessage = "\r\nYou feel a presence has departed\r\n";
                messenger.Publish(session, new NullSpaceMessage(session, keyColor, departedMessage));
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalArea;
                session.ShowPrompt = originalPrompt;                
                messenger.Unsubscribe(subscriber);
                session.Io.OutputLine($"{session.Io.NewLine}Press any key to return to the normal world.");
                Thread.Sleep(2000);
            }
        }

        private static ConsoleColor? GetKey(BbsSession session)
        {
            session.Io.OutputLine("You have found a door with a lock, beside the door is a tray.");
            var availableKeys = _keyColors.Where(k => _keys[k]).ToArray();

            if (availableKeys.Length < 1)
                session.Io.OutputLine("Unfortunately the tray is empty.");
            else if (availableKeys.Length == 1)
                session.Io.OutputLine("In the tray you see a key.");
            else
                session.Io.OutputLine("In the tray you see keys of various colors.");
            
            if (availableKeys.Length > 0)
            {
                for (int i=0; i < availableKeys.Length; i++)
                {
                    session.Io.OutputLine($"{i + 1}) Take and use the {availableKeys[i].ToString().Color(availableKeys[i])} key.");
                }
            }
            session.Io.OutputLine("Q) Quit");
            var key = session.Io.Ask("Option");
            if (int.TryParse($"{key}", out int n) && n >= 1 && n <= availableKeys.Length)
                return availableKeys[n - 1];
            return null;
        }
    }
}
