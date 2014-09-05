using System;
using System.Diagnostics;
using System.Linq;

namespace Nake
{
    /// <summary>
    /// Helper methods to control Nake runner
    /// </summary>
    public static class App
    {
        static readonly Action<int, string, Exception> Terminator = (code, msg, ex) =>
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

        /// <summary>
        /// Exits Nake runner with optional printing of the given message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Exit(string message = null)
        {
            if (message != null)
                Log.Message(message);

            Terminator(0, null, null);
        }

        /// <summary>
        /// Exits Nake runner with failure code (-1), optionally printing the given message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Fail(string message = null)
        {
            if (message != null)
                Log.Error(message);

            Terminator(-1, message, null);
        }

        internal static void Fail(Exception exception)
        {
            Terminator(-1, exception.Message, exception);
        }
    }
}
