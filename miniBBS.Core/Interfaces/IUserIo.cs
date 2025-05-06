using miniBBS.Core.Enums;
using System;

namespace miniBBS.Interfaces
{
    public interface IUserIo
    {
        string NewLine { get; }
        string Backspace { get; }
        void OutputRaw(params byte[] bytes);
        void Output(char c);
        void Output(string s, OutputHandlingFlag flags = OutputHandlingFlag.None);
        void OutputLine(string s = null, OutputHandlingFlag flags = OutputHandlingFlag.None);
        void OutputBackspace();

        byte[] InputRaw();
        char? InputKey();
        string InputLine(InputHandlingFlag handlingFlag = InputHandlingFlag.None);
        string InputLine(Func<string, string> autoComplete, InputHandlingFlag handlingFlag = InputHandlingFlag.None);
        string InputKeyOrLine();

        void ClearLine();
        void ClearScreen();
        void ResetColor();
        void SetForeground(ConsoleColor color);
        void SetBackground(ConsoleColor color);
        void SetColors(ConsoleColor background, ConsoleColor foreground);

        /// <summary>
        /// Sets the background and foreground colors.  When disposed (end of the using block) the original colors are restored
        /// </summary>
        IDisposable WithColorspace(ConsoleColor background, ConsoleColor foreground);

        ConsoleColor GetForeground();
        ConsoleColor GetBackground();

        TerminalEmulation EmulationType { get; }
        string Bold { get; }
        string Underline { get; }
        string Reversed { get; }

        string Up { get; }
        string Down { get; }
        string Left { get; }
        string Right { get; }
        string Home { get; }
        string Clear { get; }

        void SetPosition(int x, int y);

        bool IsInputting { get; }
        //Queue<Action> DelayedNotification { get; }
        void DelayNotification(Action action);

        void Flush();

        #region KeyPolling
        bool IsPollingKeys { get; }
        char? GetPolledKey();
        void PollKey();
        void ClearPolledKey();
        void AbortPollKey();
        long GetPolledTicks();
        #endregion

        byte[] GetBytes(string text);
        string TransformText(string text);

        /// <summary>
        /// For CBM mode only, switches to uppercase character set
        /// </summary>
        void SetUpper();

        /// <summary>
        /// For CBM mode only, switches to lowercase character set
        /// </summary>
        void SetLower();
    }
}
