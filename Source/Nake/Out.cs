using System;

namespace Nake
{
	internal static class Out
	{
		public static bool QuietMode;
		public static bool SilentMode;
		public static bool TraceEnabled;

		public static void TraceFormat(string message, params object[] args)
		{
			if (!TraceEnabled)
				return;

            With.Color(ConsoleColor.DarkGreen, () => Console.WriteLine(message, args));
		}

		public static void LogFormat(string message, params object[] args)
		{
			if (SilentMode)
				return;

            With.Color(ConsoleColor.DarkCyan, () => Console.WriteLine(message, args));
		}

		public static void InfoFormat(string message, params object[] args)
		{
			Info(string.Format(message, args));
		}

		public static void Info(string message)
		{
			if (QuietMode)
				return;

            With.Color(ConsoleColor.DarkGray, () => Console.WriteLine(message));
		}

		public static void Fail(Exception exception)
		{
			Fail(exception.Message);

			if (TraceEnabled)
				Info(exception.StackTrace);
		}

		public static void Fail(string message)
		{
            With.Color(ConsoleColor.DarkRed, () => Console.WriteLine(message));
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
