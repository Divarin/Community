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
        UseLastLine = 8,

        /// <summary>
        /// Remove newline (Enter) input from echo
        /// </summary>
        DoNotEchoNewlines = 16,

        /// <summary>
        /// For enter text into chat, allow CTRL+ENTER to add a blank line in the text without submitting the text
        /// </summary>
        AllowCtrlEnterToAddNewLine = 32,

        /// <summary>
        /// If the line appears to be an emote (starts with /me) then limit the input to Constants.MaxEmoteLength
        /// </summary>
        MaxLengthIfEmote = 64
    }
}
