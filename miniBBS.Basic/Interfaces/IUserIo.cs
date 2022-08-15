//using System;

//namespace miniBBS.Basic.Interfaces
//{
//    public interface IUserIo
//    {
//        void Output(char c);
//        void Output(string s);
//        void OutputLine(string s = null);

//        void Output(Func<IUserIo, string> func);
//        void OutputLine(Func<IUserIo, string> func);

//        bool IsPolling { get; }
//        void PollKey();
//        void AbortPollKey();
//        ConsoleKeyInfo GetPolledKey();
//        long GetPolledTicks();
//        void ClearPolledKey();

//        ConsoleKeyInfo InputKey();
//        string InputLine();

//        void ClearLine();
//        void ClearScreen();
//        void ResetColor();
//        void SetForeground(ConsoleColor color);
//        void SetBackground(ConsoleColor color);
//        void SetColors(ConsoleColor background, ConsoleColor foreground);

//        /// <summary>
//        /// Sets the background and foreground colors.  When disposed (end of the using block) the original colors are restored
//        /// </summary>
//        IDisposable WithColorspace(ConsoleColor background, ConsoleColor foreground);

//        ConsoleColor GetForeground();
//        ConsoleColor GetBackground();

//        string Bold { get; }
//        string Underline { get; }
//        string Reversed { get; }

//        string Up { get; }
//        string Down { get; }
//        string Left { get; }
//        string Right { get; }

//        void SetPosition(int x, int y);

//    }
//}
