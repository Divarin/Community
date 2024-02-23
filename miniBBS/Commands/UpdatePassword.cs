using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Commands
{
    public static class UpdatePassword
    {
        public static bool Execute(BbsSession session, bool allowKeepCurrent)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.Output("Password change utility.");  
                if (allowKeepCurrent)
                    session.Io.Output("Enter a blank password to abort and keep your current password.");
                session.Io.OutputLine();

                var didGetNewPass = GetNewPasswordHash(session, allowKeepCurrent, out var newPasswordHash);
                if (!didGetNewPass)
                    return false;
                
                session.User.PasswordHash = newPasswordHash;
                session.UserRepo.Update(session.User);
                session.Io.SetForeground(ConsoleColor.Green);
                session.Io.OutputLine("Password updated!  Don't forget it!!!");
                return true;
            }
        }
    
        public static bool GetNewPasswordHash(BbsSession session, bool allowKeepCurrent, out string newPassword)
        {
            newPassword = null;
            while (newPassword == null)
            {
                session.Io.Output("Enter new password: ");
                newPassword = session.Io.InputLine(InputHandlingFlag.PasswordInput);
                session.Io.OutputLine();
                if (allowKeepCurrent && string.IsNullOrWhiteSpace(newPassword))
                {
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.OutputLine(" *** ABORTED! ***");
                    newPassword = null;
                    return false;
                }
            }

            if (newPassword.Length < Constants.MinimumPasswordLength)
            {
                session.Io.SetForeground(ConsoleColor.Red);
                session.Io.OutputLine($"Password too short, must be at least {Constants.MinimumPasswordLength} characters.");
                newPassword = null;
                return false;
            }

            newPassword = newPassword.ToLower();
            session.Io.Output("Enter it again (to check for typos): ");
            var newPass2 = session.Io.InputLine(InputHandlingFlag.PasswordInput);
            session.Io.OutputLine();
            newPass2 = newPass2?.ToLower();

            if (newPassword.Equals(newPass2))
            {
                newPassword = DI.Get<IHasher>().Hash(newPassword);
                return true;
            }

            session.Io.SetForeground(ConsoleColor.Red);
            session.Io.OutputLine("Nope, you typoed at least one of those, they don't match.  Password not updated.");
            newPassword = null;
            return false;
        }
    }
}
