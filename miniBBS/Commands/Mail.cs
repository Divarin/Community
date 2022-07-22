using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Mail
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            var originalLocation = session.CurrentLocation;
            session.CurrentLocation = Module.Email;

            try
            {
                if (true != args?.Any())
                {
                    PrintUsage(session);
                    return;
                }

                IList<Core.Models.Data.Mail> mails = GetMails(session);

                switch (args[0].ToLower())
                {
                    case "list":
                        if (args.Length >= 2 && "sent".Equals(args[1], StringComparison.CurrentCultureIgnoreCase))
                            ListSentMails(session);
                        else
                            ListMails(session, mails);
                        break;
                    case "read":
                        {
                            if (args.Length >= 2 && int.TryParse(args[1], out int n) && n >= 1 && n <= mails.Count)
                                ReadMail(session, mails[n - 1]);
                            else
                                Error(session, "Invalid message number, please type '/mail read 123' where '123' is the message number.");
                        }
                        break;
                    case "del":
                        {
                            if (args.Length >= 2 && int.TryParse(args[1], out int n) && n >= 1 && n <= mails.Count)
                                DeleteMail(session, mails[n - 1]);
                            else
                                Error(session, "Invalid message number, please type '/mail read 123' where '123' is the message number.");
                        }
                        break;
                    case "send":
                        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
                            SendMail(session, args[1].Trim());
                        else
                            Error(session, "Please provide who the send the mail to: /mail send jimbob");
                        break;
                    case "feedback":
                        SendMail(session, Constants.SysopName);
                        break;
                    default:
                        PrintUsage(session);
                        break;
                }
            }
            finally
            {
                session.CurrentLocation = originalLocation;
            }
        }

        public static int CountUnread(BbsSession session)
        {
            return DI.GetRepository<Core.Models.Data.Mail>()
                .Get(m => m.ToUserId, session.User.Id)
                .Count(c => !c.Read);
        }

        private static void SendMail(BbsSession session, string to)
        {
            var toId = session.Usernames.FirstOrDefault(x => to.Equals(x.Value, StringComparison.CurrentCultureIgnoreCase)).Key;
            if (toId < 0)
            {
                Error(session, $"Unknown user '{to}'");
                return;
            }

            SendMail(session, toId);
        }

        private static void SendMail(BbsSession session, int toId, string subject="")
        { 
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                if (!string.IsNullOrWhiteSpace(subject))
                    session.Io.Output($"Subject (ENTER='{subject}'): ");
                else
                    session.Io.Output("Subject: ");

                var newSubject = session.Io.InputLine();
                if (!string.IsNullOrWhiteSpace(newSubject))
                    subject = newSubject;

                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(subject))
                {
                    Error(session, "Aborted!");
                    return;
                }
                session.Io.OutputLine("Type your message below.  On a blank line type /s to send or /a to abort.");
                session.Io.SetForeground(ConsoleColor.White);
                StringBuilder builder = new StringBuilder();
                do
                {
                    string line = session.Io.InputLine();
                    if ("/s".Equals(line, StringComparison.CurrentCultureIgnoreCase))
                    {
                        SendMail(session, toId, subject, builder.ToString());
                        session.Io.SetForeground(ConsoleColor.Yellow);
                        session.Io.OutputLine("Mail sent!");
                        session.Messager.Publish(new UserMessage(session.Id, toId, $"New mail from {session.User.Name}"));
                        break;
                    }
                    else if ("/a".Equals(line, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Error(session, "Aborted!");
                        break;
                    }
                    else
                        builder.AppendLine(line);
                } while (true);
            }
        }

        private static void SendMail(BbsSession session, int toId, string subject, string message)
        {
            DI.GetRepository<Core.Models.Data.Mail>().Insert(new Core.Models.Data.Mail
            {
                ToUserId = toId,
                FromUserId = session.User.Id,
                SentUtc = DateTime.UtcNow,
                Subject = subject,
                Message = message
            });
        }

        public static void SysopFeedback(BbsSession session, string subject, string feedback)
        {
            var originalLocation = session.CurrentLocation;
            session.CurrentLocation = Module.Email;

            try
            {
                int toId = DI.GetRepository<Core.Models.Data.User>()
                    .Get(u => u.Name, Constants.SysopName)
                    .First()
                    .Id;

                SendMail(session, toId, subject, feedback);
            }
            finally
            {
                session.CurrentLocation = originalLocation;
            }
        }

        private static void ReadMail(BbsSession session, Core.Models.Data.Mail mail)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                var from = session.Usernames.ContainsKey(mail.FromUserId) ? session.Usernames[mail.FromUserId] : "Unknown " + mail.FromUserId.ToString();
                var to = session.Usernames.ContainsKey(mail.ToUserId) ? session.Usernames[mail.ToUserId] : "Unknown " + mail.ToUserId.ToString();

                session.Io.SetForeground(ConsoleColor.Magenta);
                session.Io.OutputLine($"Sent    : {mail.SentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}");
                session.Io.OutputLine($"From    : {from}");
                session.Io.OutputLine($"To      : {to}");
                session.Io.OutputLine($"Subject : {mail.Subject}");
                session.Io.SetForeground(ConsoleColor.Green);
                session.Io.OutputLine(" --------- ");
                session.Io.SetForeground(ConsoleColor.White);
                session.Io.OutputLine(mail.Message);

                if (mail.ToUserId == session.User.Id && !mail.Read)
                {
                    mail.Read = true;
                    DI.GetRepository<Core.Models.Data.Mail>().Update(mail);
                }

                session.Io.SetForeground(ConsoleColor.Yellow);
                session.Io.Output($"{(mail.ToUserId == session.User.Id ? "(R)eply, " : "")}(D)elete, (ENTER)=Continue");
                var k = session.Io.InputKey();
                session.Io.OutputLine();
                if (k.HasValue)
                {
                    switch (char.ToUpper(k.Value))
                    {
                        case 'R':
                            if (mail.ToUserId == session.User.Id)
                                SendMail(session, mail.FromUserId, $"re: {mail.Subject.Replace("re: ", "")}");
                            break;
                        case 'D':
                            DeleteMail(session, mail);
                            break;
                    }
                }
            }
        }

        private static void ListMails(BbsSession session, IList<Core.Models.Data.Mail> mails)
        {
            if (true == mails?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < mails.Count; i++)
                    {
                        var mail = mails[i];
                        var from = session.Usernames.ContainsKey(mail.FromUserId) ? session.Usernames[mail.FromUserId] : "Unknown";
                        builder.AppendLine($"{i + 1} : {from} : {(mail.Read ? "" : "(UNREAD)")} {mail.Subject}   {mail.SentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}");
                    }
                    session.Io.OutputLine(builder.ToString());
                    session.Io.SetForeground(ConsoleColor.Yellow);
                    session.Io.Output("# to read or ENTER=quit: ");
                    var inp = session.Io.InputLine();
                    session.Io.OutputLine();
                    if (!string.IsNullOrWhiteSpace(inp) && int.TryParse(inp, out int n) && n >= 1 && n <= mails.Count)
                        ReadMail(session, mails[n - 1]);
                }
            }
            else
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                {
                    session.Io.OutputLine("You have no mail, sorry.");
                }
            }
        }

        private static void ListSentMails(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                var sentMails = DI.GetRepository<Core.Models.Data.Mail>()
                    .Get(m => m.FromUserId, session.User.Id)
                    .OrderByDescending(m => m.SentUtc)
                    .ToList();

                StringBuilder builder = new StringBuilder();
                for (int i=0; i < sentMails.Count; i++)
                {
                    var mail = sentMails[i];
                    var to = session.Usernames.ContainsKey(mail.ToUserId) ? session.Usernames[mail.ToUserId] : "Unknown";
                    builder.AppendLine($"{i+1} : {to} : {(mail.Read ? "" : "(UNREAD)")} {mail.Subject}   {mail.SentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}");
                }

                session.Io.OutputLine(builder.ToString());
                session.Io.SetForeground(ConsoleColor.Yellow);
                session.Io.Output("# to read or ENTER=quit: ");
                var inp = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(inp) && int.TryParse(inp, out int n) && n >= 1 && n <= sentMails.Count)
                    ReadMail(session, sentMails[n - 1]);
            }
        }
        private static void DeleteMail(BbsSession session, Core.Models.Data.Mail mail)
        {
            var mailRepo = DI.GetRepository<Core.Models.Data.Mail>();
            mailRepo.Delete(mail);
        }

        private static IList<Core.Models.Data.Mail> GetMails(BbsSession session)
        {
            var mailRepo = DI.GetRepository<Core.Models.Data.Mail>();
            return GetMails(session, mailRepo);
        }

        private static IList<Core.Models.Data.Mail> GetMails(BbsSession session, IRepository<Core.Models.Data.Mail> mailRepo)
        { 
            var mails = mailRepo
                .Get(m => m.ToUserId, session.User.Id)
                .OrderByDescending(m => m.SentUtc);
            return mails.ToList();
        }

        private static void PrintUsage(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                session.Io.OutputLine("Mail subsystem usage:");
                session.Io.OutputLine("/mail list        : Lists your mail.");
                session.Io.OutputLine("/mail list sent   : Lists mail you've sent.");
                session.Io.OutputLine("/mail read n      : Reads message #n.");
                session.Io.OutputLine("/mail del n       : Deletes message #n.");
                session.Io.OutputLine("/mail send jimbob : Sends mail to the user 'jimbob'.");
                session.Io.OutputLine($"/mail feedback    : Same as '/mail send {Constants.SysopName}.");
                session.Io.OutputLine("/feedback         : Same as '/mail feedback'.");
            }
        }

        private static void Error(BbsSession session, string err)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                session.Io.OutputLine(err);
            }
        }
    }
}
