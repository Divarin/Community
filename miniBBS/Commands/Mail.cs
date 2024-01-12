using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
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
            var originalDnd = session.DoNotDisturb;
            
            session.CurrentLocation = Module.Email;
            session.DoNotDisturb = true;

            try
            {
                IList<Core.Models.Data.Mail> mails = GetMails(session);

                string command = args?.Length >= 1 ? args[0] : null;
                string arg = args?.Length >= 2 ? args[1] : null;

                do
                {
                    switch (command?.ToLower())
                    {
                        case "list":
                            if ("sent".Equals(arg, StringComparison.CurrentCultureIgnoreCase))
                            {
                                ListSentMails(session);
                                mails = GetMails(session);
                            }
                            else
                            {
                                ListMails(session, mails);
                                mails = GetMails(session);
                            }
                            command = null;
                            break;
                        case "read":
                            {
                                if (int.TryParse(arg, out int n) && n >= 1 && n <= mails.Count)
                                    ReadMail(session, mails[n - 1]);
                                else
                                    Error(session, "Invalid message number, please type '/mail read 123' where '123' is the message number.");
                            }
                            command = null;
                            break;
                        case "del":
                            {
                                if (int.TryParse(arg, out int n) && n >= 1 && n <= mails.Count)
                                    DeleteMail(session, mails[n - 1]);
                                else if ("all".Equals(arg, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (true == mails?.Any())
                                        DeleteAllMail(session, mails);
                                    else
                                        session.Io.Error("You have no e-mail to delete.");
                                }
                                else
                                    Error(session, "Invalid message number, please type '/mail read 123' where '123' is the message number.");
                            }
                            mails = GetMails(session);
                            command = null;
                            break;
                        case "send":
                            if (!string.IsNullOrWhiteSpace(arg))
                            {
                                SendMail(session, arg.Trim());
                                mails = GetMails(session);
                            }
                            else
                                Error(session, "Please provide who the send the mail to: /mail send jimbob");
                            command = null;
                            break;
                        case "feedback":
                            SendMail(session, Constants.SysopName);
                            mails = GetMails(session);
                            command = null;
                            break;
                        default:
                            {
                                var tuple = Menu(session);
                                if (tuple?.Item1 != null)
                                {
                                    command = tuple.Item1;
                                    arg = tuple.Item2;
                                }
                            }
                            break;
                    }
                } while (command != "quit");

            }
            finally
            {
                session.CurrentLocation = originalLocation;
                session.DoNotDisturb = originalDnd;
            }
        }

        private static Tuple<string, string> Menu(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine($"{Constants.Inverser} *** E-Mail ***{Constants.Inverser}");
                session.Io.SetForeground(ConsoleColor.Yellow);
                session.Io.OutputLine("L".Color(ConsoleColor.Green) + $") List your mail (+read & delete)");
                session.Io.OutputLine("O".Color(ConsoleColor.Green) + $") List outgoing (sent) mails");
                session.Io.OutputLine("D".Color(ConsoleColor.Green) + $") Delete all of your mail");
                session.Io.OutputLine("S".Color(ConsoleColor.Green) + $") Send mail");
                session.Io.OutputLine("F".Color(ConsoleColor.Green) + $") Feedback to Sysop");
                session.Io.OutputLine("Q".Color(ConsoleColor.Green) + $") Quit E-Mail");

                var k = session.Io.Ask("[Mail]");
                switch (k)
                {
                    case 'L': return new Tuple<string, string>("list", null);
                    case 'O': return new Tuple<string, string>("list", "sent");
                    case 'D': return new Tuple<string, string>("del", "all");
                    case 'S':
                        {
                            session.Io.Output("Send to whom?: ");
                            string toUsername = session.Io.InputLine();
                            session.Io.OutputLine();
                            if (!string.IsNullOrWhiteSpace(toUsername))
                                return new Tuple<string, string>("send", toUsername);
                        }
                        return null;
                    case 'F': return new Tuple<string, string>("feedback", null);
                    case 'Q': return new Tuple<string, string>("quit", null);
                }
            }
            return new Tuple<string, string>(null, null);
        }

        public static Count Count(BbsSession session)
        {
            var mails = DI.GetRepository<Core.Models.Data.Mail>()
                .Get(m => m.ToUserId, session.User.Id);

            return new Count
            {
                TotalCount = mails.Count(),
                SubsetCount = mails.Count(c => !c.Read)
            };
        }

        private static void SendMail(BbsSession session, string to)
        {
            var toId = session.Usernames.FirstOrDefault(x => to.Equals(x.Value, StringComparison.CurrentCultureIgnoreCase)).Key;
            if (toId <= 0)
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
                if (session.Usernames.ContainsKey(toId))
                {
                    session.Io.SetForeground(ConsoleColor.White);
                    session.Io.OutputLine($"Sending mail to {UserIoExtensions.WrapInColor(session.Usernames[toId], ConsoleColor.Yellow)}");
                    session.Io.SetForeground(ConsoleColor.Magenta);
                }

                if (!string.IsNullOrWhiteSpace(subject))
                    session.Io.Output($"{Constants.Inverser}Subject (ENTER='{subject}'):{Constants.Inverser} ");
                else
                    session.Io.Output($"{Constants.Inverser}Subject:{Constants.Inverser} ");

                var newSubject = session.Io.InputLine();
                if (!string.IsNullOrWhiteSpace(newSubject))
                    subject = newSubject;

                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(subject))
                {
                    Error(session, "Aborted!");
                    return;
                }

                var lineEditor = DI.Get<ITextEditor>();
                lineEditor.OnSave = _body =>
                {
                    SendMail(session, toId, subject, _body);
                    session.Messager.Publish(session, new UserMessage(session.Id, toId, $"New mail from {session.User.Name}, use /mr to read."));
                    return "Mail sent!";
                };
                lineEditor.EditText(session, new LineEditorParameters
                {
                    QuitOnSave = true
                });
            }
        }

        public static void SendMail(BbsSession session, int toId, string subject, string message)
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
                session.Io.OutputLine($"{Constants.Inverser}Sent    :{Constants.Inverser} {mail.SentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}");
                session.Io.OutputLine($"{Constants.Inverser}From    :{Constants.Inverser} {from}");
                session.Io.OutputLine($"{Constants.Inverser}To      :{Constants.Inverser} {to}");
                session.Io.OutputLine($"{Constants.Inverser}Subject :{Constants.Inverser} {mail.Subject}");
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
                session.Io.Output($"{Constants.Inverser}{(mail.ToUserId == session.User.Id ? "(R)eply, " : "")}{(!mail.Read && mail.FromUserId == session.User.Id ? "(E)dit, ": "")}(D)elete, (ENTER)=Continue{Constants.Inverser}: ");
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
                        case 'E':
                            if (mail.FromUserId == session.User.Id && !mail.Read)
                                EditMail(session, mail);
                            break;
                        case 'D':
                            if (mail.ToUserId == session.User.Id || (!mail.Read && mail.FromUserId == session.User.Id))
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
                    session.Io.Output($"{Constants.Inverser}# to read or ENTER=quit:{Constants.Inverser} ");
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
                session.Io.OutputLine("D# : Delete message # (if not yet read)");
                session.Io.OutputLine("E# : Edit message # (if not yet read)");
                session.Io.OutputLine("#  : Read message #");
                session.Io.Output($"{Constants.Inverser}# to read or ENTER=quit:{Constants.Inverser} ");
                var inp = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(inp))
                {
                    if (int.TryParse(inp, out int n) && n >= 1 && n <= sentMails.Count)
                        ReadMail(session, sentMails[n - 1]);
                    else if (inp.Length >= 2 && int.TryParse(inp.Substring(1), out n) && n >= 1 && n <= sentMails.Count)
                    {
                        var mailMessage = sentMails[n - 1];
                        if (mailMessage.Read)
                        {
                            session.Io.Error("Message has already been read.");
                        }
                        else
                        {
                            switch (inp[0])
                            {
                                case 'd':
                                case 'D':
                                    DeleteMail(session, mailMessage);
                                    break;
                                case 'e':
                                case 'E':
                                    EditMail(session, mailMessage);
                                    break;
                                default:
                                    session.Io.Error("Unrecognized command");
                                    break;
                            }
                        }
                    }
                }
                
            }
        }

        private static void EditMail(BbsSession session, Core.Models.Data.Mail mail)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                if (session.Usernames.ContainsKey(mail.ToUserId))
                {
                    session.Io.SetForeground(ConsoleColor.White);
                    session.Io.OutputLine($"Editing mail to {UserIoExtensions.WrapInColor(session.Usernames[mail.ToUserId], ConsoleColor.Yellow)}");
                    session.Io.SetForeground(ConsoleColor.Magenta);
                }

                session.Io.Output($"{Constants.Inverser}Subject (ENTER='{mail.Subject}'):{Constants.Inverser} ");
                var newSubject = session.Io.InputLine();
                if (!string.IsNullOrWhiteSpace(newSubject))
                    mail.Subject = newSubject;
                session.Io.OutputLine();

                var lineEditor = DI.Get<ITextEditor>();
                lineEditor.OnSave = _body =>
                {
                    mail.Message = _body;
                    DI.GetRepository<Core.Models.Data.Mail>().Update(mail);
                    return "Mail edited";
                };
                lineEditor.EditText(session, new LineEditorParameters
                {
                    QuitOnSave = true,
                    PreloadedBody = mail.Message,
                });
            }
        }

        private static void DeleteMail(BbsSession session, Core.Models.Data.Mail mail)
        {
            session.Io.OutputLine($"Are you sure you want to delete this mail:{Environment.NewLine}From: {session.Username(mail.FromUserId)}, To: {session.Username(mail.ToUserId)}{Environment.NewLine}Subject: {mail.Subject}");
            if ('Y' == session.Io.Ask("Delete?"))
            {
                var mailRepo = DI.GetRepository<Core.Models.Data.Mail>();
                mailRepo.Delete(mail);
            }
        }

        private static void DeleteAllMail(BbsSession session, IEnumerable<Core.Models.Data.Mail> mails)
        {
            if (true != mails?.Any())
            {
                session.Io.Error("You don't have any mail.");
                return;
            }
            var key = session.Io.Ask("Delete: (A)ll Mail or only (R)ead mail, (Q)uit");
            switch (key)
            {
                case 'A':
                    DI.GetRepository<Core.Models.Data.Mail>().DeleteRange(mails);
                    break;
                case 'R':
                    DI.GetRepository<Core.Models.Data.Mail>().DeleteRange(mails.Where(x => x.Read).ToList());
                    break;
            }
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
                session.Io.OutputLine($"{Constants.Inverser}Mail subsystem usage:{Constants.Inverser}");
                session.Io.OutputLine("/mail list        : Lists your mail.");
                session.Io.OutputLine("/mail list sent   : Lists mail you've sent.");
                session.Io.OutputLine("/mail read n      : Reads message #n.");
                session.Io.OutputLine("/mail del n       : Deletes message #n.");
                session.Io.OutputLine("/mail del all     : Deletes all messages.");
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

        internal static void ReadLatest(BbsSession session)
        {
            var originalLocation = session.CurrentLocation;
            var originalDnd = session.DoNotDisturb;

            session.CurrentLocation = Module.Email;
            session.DoNotDisturb = true;

            try
            {
                var latest = GetMails(session).FirstOrDefault();
                if (latest == null)
                {
                    session.Io.Error("No mail!");
                    return;
                }
                ReadMail(session, latest);
            }
            finally
            {
                session.CurrentLocation = originalLocation;
                session.DoNotDisturb = originalDnd;
            }
        }
    }
}
