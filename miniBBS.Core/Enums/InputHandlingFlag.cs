using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum InputHandlingFlag
    {
        /// <summary>
        /// Normal input handling
        /// </summary>
        None = 0,

        /// <summary>
        /// Echos * instead of typed characters
        /// </summary>
        PasswordInput = 1,

        /// <summary>
        /// Allows use of single character commands such as [ ] < > ?
        /// </summary>
        InterceptSingleCharacterCommand = 2,

        /// <summary>
        /// Allows use of tab to auto-complete
        /// </summary>
        AutoCompleteOnTab = 4,

        /// <summary>
        /// Allows use of up arrow to recall last typed line
        /// </summary>
        UseLastLine = 8
    }
}
