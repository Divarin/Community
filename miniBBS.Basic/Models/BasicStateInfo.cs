using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Models
{
    public class BasicStateInfo
    {
        /// <summary>
        /// The session running Basic
        /// </summary>
        public BbsSession Session { get; set; }

        /// <summary>
        /// The full path and filename to the .BAS, .BOT, or .MBS file
        /// </summary>
        public string ProgramPath { get; set; }

        /// <summary>
        /// True if the program is running, false if just in the editor
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// True if running from the editor, false if ran outside of the editor
        /// </summary>
        public bool IsInEditor { get; set; }

        /// <summary>
        /// Variables
        /// </summary>
        public Variables Variables { get; set; }
    }
}
