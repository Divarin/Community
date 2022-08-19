using miniBBS.Core.Models.Control;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class SetMessagePointer
    {
        /// <summary>
        /// Tries to set the msg pointer to the <paramref name="msgPointer"/> or the nearest next message in the active channel. 
        /// Returns true if the message pointer has changed or false if it did not (at end of messages). 
        /// Also saves the user's channel's msg pointer in the database for their next login
        /// </summary>
        public static bool Execute(BbsSession session, int msgPointer)
        {
            int oldMsgPointer = session.MsgPointer;

            if (true != session.Chats?.Any())
                msgPointer = 0;
            else
            {
                msgPointer = session.Chats.Keys.FirstOrDefault(k => k >= msgPointer);
                if (msgPointer == default)
                    msgPointer = session.Chats.Keys.Max();
            }

            bool changed = msgPointer != oldMsgPointer;

            if (changed)
            {
                session.UcFlag.LastReadMessageNumber = msgPointer;
                session.UcFlag = session.UcFlagRepo.InsertOrUpdate(session.UcFlag);
            }

            session.ContextPointer = null;
            return changed;
        }
    }
}
