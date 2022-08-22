using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum InputHandlingFlag
    {
        None = 0,
        PasswordInput = 1,
        InterceptSingleCharacterCommand = 2,
        AutoCompleteOnTab = 4
    }
}
