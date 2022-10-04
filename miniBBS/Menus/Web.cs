using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Web
    {
        private static readonly string[] _lines = new[]
        {
            "*** Web-Posts ***",
            $"{Constants.Spaceholder}",
            "To help increase the visibility of this BBS and get more people to call some chats are visible on the web at http://mutinybbs.com/chatlog.html.  " +
            "As your Sysop I appreciate and am very much sympathetic with those users who don't want their conversations here on Community exposed to web spiders such as Google.  " +
            "Therefore I have taken great care to implement constraints on what does and does not get put up on the web.",
            $"{Constants.Spaceholder}",
            "* Messages in channels that require an invite are never visible on the web.",
            "* If you set your preference to never have your messages on the web then they won't be unless you override that by starting your message with the '/web' command.  (example: '/web Hi, this message will be on the web!')",
            "* A channel can also have a preference set by the channel moderator.  The main channel [General] has no preference so messages are not automatically put on the web.",
            "* Your personal preference trumps the channel's preference",
            "* You can remove messages you've posted from the web using /noweb (explained below)",
            "* You can add messages to the web you've posted in the past using /web (explained below)",
            "* Moderators may remove messages you've posted from the web but only you can add them to the web",
            $"{Constants.Spaceholder}",
            "--- Web-Control Commands---",
            "/ch +w : As a channel moderator you can use this to have your channel 'prefer' to have messages on the web.  This will put on the web messages posted by users who have not explicitly stated they don't want their messages on the web.",
            "/ch -w : As a channel moderator you can use this to undo the effects of '/ch +w' (above)",
            "/web : Sets your preference to have your future messages be visible on the web.",
            "/noweb : Sets your preference to NOT have your future messages be visible on the web.",
            "/web (message) : Has no effect on your preference but posts the (message) to the channel and also to the web.",
            "/noweb (message) : Regardless of your preference or the channel's preference the (message) you post will only be shown in the channel and NOT on the web.",
            "/newweb (message) : A combination of the /new and /web commands.  Also: /webnew",
            "/newnoweb (message) : A combination of the /new and /noweb commands.  Also: /nowebnew",
            "/web # : If message # is yours then it makes that message web-visible.",
            "/noweb # : If message # is yours (or if you're a moderator) then makes that message NOT web-visible.",
            "/webpref : Shows your current preferences about posting to the web and lets you change them.  This also shows the current channel's preference",
            $"{Constants.Spaceholder}",
            $"Please note that posts are only added to, or removed from, the web once every {Constants.WebLogRefreshDelay.TotalHours} hours."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
