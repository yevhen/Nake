using System;

namespace Nake
{
    public static class Log
    {
        public static Action<string> Out = text => Console.WriteLine(text);

        static readonly bool QuietMode = Env.Defined("NakeQuietMode");
        static readonly bool SilentMode = Env.Defined("NakeSilentMode");
        static readonly bool TraceEnabled = Env.Defined("NakeTraceEnabled");

        public static void TraceFormat(string message, params object[] args)
        {
            Trace(string.Format(message, args));
        }

        public static void Trace(string message)
        {
            if (!TraceEnabled)
                return;

            With.Color(ConsoleColor.DarkGreen, () => Out(message));
        }

        public static void MessageFormat(string message, params object[] args)
        {
            Message(string.Format(message, args));
        }

        public static void Message(string message)
        {
            if (SilentMode)
                return;

            With.Color(ConsoleColor.DarkCyan, () => Out(message));
        }

        public static void InfoFormat(string message, params object[] args)
        {
            Info(string.Format(message, args));
        }

        public static void Info(string message)
        {
            if (QuietMode)
                return;

            With.Color(ConsoleColor.DarkGray, () => Out(message));
        }

        public static void Error(Exception exception)
        {
            Error(exception.Message);

            if (TraceEnabled)
                Info(exception.StackTrace);
        }

        public static void Error(string message)
        {
            With.Color(ConsoleColor.DarkRed, () => Out(message));
        }
    }

    static class With
    {
        public static void Color(ConsoleColor color, Action action)
        {
            Console.ForegroundColor = color;

            action();

            Console.ResetColor();
        }
    }
}
