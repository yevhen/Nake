using System;

namespace Nake
{
    public static class Exit
    {
        internal static Action<int, string, Exception> Terminator = (code, msg, ex) => Environment.Exit(code);

        public static void Ok()
        {
            Terminator(0, "", null);
        }

        public static void Fail(string message)
        {
            Environment.Exit(-1);
        }

        public static void Fail(Exception exception)
        {
            Terminator(-1, exception.Message, exception);
        }
    }
}
