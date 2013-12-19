using System;
using System.Diagnostics;

namespace Nake
{
    public static class Exit
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

        public static void Ok()
        {
            Terminator(0, null, null);
        }

        public static void Fail()
        {
            Terminator(-1, null, null);
        }

        public static void Fail(string message)
        {
            Terminator(-1, message, null);
        }

        public static void Fail(Exception exception)
        {
            Terminator(-1, exception.Message, exception);
        }
    }
}
