using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;

namespace miniBBS.Commands
{
    public static class UserPreferences
    {
        public static void Execute(BbsSession session)
        {
            var metaRepo = DI.GetRepository<Metadata>();
            do
            {
                var meta = metaRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.UserId), session.User.Id},
                    {nameof(Metadata.Type), MetadataType.LoginStartupMode}
                })?.PruneAllButMostRecent(metaRepo);

                LoginStartupMode mode = LoginStartupMode.MainMenu;
                if (!string.IsNullOrWhiteSpace(meta?.Data) && Enum.TryParse(meta.Data, out LoginStartupMode lsm))
                    mode = lsm;

                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                {
                    session.Io.OutputLine($"1) Login Startup Mode: {mode.FriendlyName()}");
                }

                session.Io.Output("[PREFS] : ".Color(ConsoleColor.White));
                var inp = session.Io.InputKey();
                session.Io.OutputLine();
                if (inp == '1')
                {
                    mode = mode == LoginStartupMode.ChatRooms ? LoginStartupMode.MainMenu : LoginStartupMode.ChatRooms;
                    meta.Data = mode.ToString();
                    metaRepo.Update(meta);
                }
                else
                    break;
            } while (true);
        }

    }
}
