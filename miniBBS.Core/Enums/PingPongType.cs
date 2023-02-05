namespace miniBBS.Core.Enums
{
    public enum PingPongType
    {
        /// <summary>
        /// Prints "(newline)Ping? Pong!"
        /// </summary>
        Full,

        /// <summary>
        /// Prints ".", (backspace)
        /// </summary>
        Invisible,

        /// <summary>
        /// Prints 3-7 newlines, then 5-10 spaces, then [Mutiny Community (timestamp)]
        /// </summary>
        ScreenSaver
    }
}
