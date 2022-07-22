using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Interfaces
{
    public interface ITextFilesBrowser
    {
        /// <summary>
        /// An action to perform when the user does a 'chat' command, this should post the chat message to the channel
        /// </summary>
        Action<string> OnChat { get; set; }
        void Browse(BbsSession session);

        /// <summary>
        /// Returns true if a link was found in the <paramref name="msg"/>
        /// </summary>
        bool ReadLink(BbsSession session, string msg);
    }
}
