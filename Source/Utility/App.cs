using System;
using System.Diagnostics;
using System.Linq;

namespace Nake
{
    public static class App
    {
        internal static Action<int, string, Exception> Terminator = (code, msg, ex) =>
        {
            if (Debugger.IsAttached)
                WaitTermination();

            Environment.Exit(code);
        };

        static void WaitTermination()
        {
            Console.Write("Press any key to terminate ...");
            Console.ReadKey();
        }

        public static void Exit(string message = null)
        {
            if (message != null)
                Log.Message(message);

            Terminator(0, null, null);
        }

        public static void Fail(string message = null)
        {
            if (message != null)
                Log.Message(message);

            Terminator(-1, message, null);
        }

        internal static void Fail(Exception exception)
        {
            Terminator(-1, exception.Message, exception);
        }
    }
}
