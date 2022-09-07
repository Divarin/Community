using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;

namespace miniBBS.Core.Interfaces
{
    public interface ITextFilesBrowser
    {
        /// <summary>
        /// An action to perform when the user does a 'chat' command, this should post the chat message to the channel
        /// </summary>
        Action<string> OnChat { get; set; }
        void Browse(BbsSession session, FilesLaunchFlags flags = FilesLaunchFlags.None);

        /// <summary>
        /// Returns true if a link was found in the <paramref name="msg"/>
        /// </summary>
        bool ReadLink(BbsSession session, string msg);

        /// <summary>
        /// Searches the CommunityUsers directories for published BASIC (.bas) programs and brings back links to them.
        /// </summary>
        IEnumerable<string> FindBasicPrograms(BbsSession session);
    }
}
