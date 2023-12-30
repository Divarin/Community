using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Subscribers;
using miniBBS.UserIo;
using System;
using System.Threading;

namespace miniBBS.Commands
{
    public static class NullSpace
    {
        public static void Enter(BbsSession session)
        {
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
                messenger.Publish(session, new NullSpaceMessage(session, enteredMessage));

                session.Io.PollKey();
                var lastKeyPoll = session.Io.GetPolledTicks();
                var exit = false;
                while (!exit)
                {
                    while (lastKeyPoll >= session.Io.GetPolledTicks())
                    {
                        Thread.Sleep(25);
                    }
                    var key = session.Io.GetPolledKey();
                    lastKeyPoll = session.Io.GetPolledTicks();
                    if (!key.HasValue)
                        continue;

                    var isEscOrCtrlC =
                        key == 3 ||
                        key == 27 ||
                        (session.Io is Atascii && (key == 30 || key == 0));

                    if (isEscOrCtrlC)
                        exit = true;

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

                    messenger.Publish(session, new NullSpaceMessage(session, msg));
                    session.Io.Output(msg);
                }
            }
            finally
            {
                session.Io.AbortPollKey();

                var departedMessage = "\r\nYou feel a presence has departed\r\n";
                messenger.Publish(session, new NullSpaceMessage(session, departedMessage));
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalArea;
                session.ShowPrompt = originalPrompt;                
                messenger.Unsubscribe(subscriber);
                session.Io.OutputLine($"{session.Io.NewLine}Press any key to return to the normal world.");
                Thread.Sleep(2000);
            }
        }
    }
}
