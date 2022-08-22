using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Commands
{
    public static class UpdatePassword
    {
        public static void Execute(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine("Password change utility.  Enter a blank password to abort and keep your current password.");
                session.Io.Output("Enter new password: ");
                var newPass = session.Io.InputLine(InputHandlingFlag.PasswordInput);
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(newPass))
                {
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.OutputLine(" *** ABORTED! ***");
                    return;
                }

                if (newPass.Length < Constants.MinimumPasswordLength)
                {
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.OutputLine($"Password too short, must be at least {Constants.MinimumPasswordLength} characters.");
                    return;
                }

                newPass = newPass.ToLower();
                session.Io.Output("Enter it again: ");
                var newPass2 = session.Io.InputLine(InputHandlingFlag.PasswordInput);
                session.Io.OutputLine();
                newPass2 = newPass2?.ToLower();

                if (!newPass.Equals(newPass2))
                {
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.OutputLine("Nope, you typoed at least one of those, they don't match.  Password not updated.");
                    return;
                }

                var hash = DI.Get<IHasher>().Hash(newPass);
                session.User.PasswordHash = hash;
                session.UserRepo.Update(session.User);
                session.Io.SetForeground(ConsoleColor.Green);
                session.Io.OutputLine("Password updated!  Don't forget it!!!");
            }
        }
    }
}
