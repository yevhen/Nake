﻿using System;

namespace Nake
{
    /// <summary>
    /// Shortcut methods for outputting messages to std out
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Controls actual printing to Console.Out.
        /// </summary>
        /// <remarks>
        ///  Could be substituted in order to redirect messages
        /// </remarks>
        public static Action<string> Out = Console.WriteLine;

        /// <summary>
        /// Enables tracing of all messages sent to <code>Log.Out</code>. 
        /// </summary>
        /// <example>
        /// using (var trace = Log.TraceOut())
        /// {
        ///     Run.Cmd("NuGet.exe list");
        ///     var nugetOutput = trace.Out; // we traced all nuget console output
        /// }
        /// </example>
        public static Tracer TraceOut()
        {
            return new Tracer(ref Out);
        }

        static readonly bool QuietMode = Env.Var.Defined("NakeQuietMode");
        static readonly bool SilentMode = Env.Var.Defined("NakeSilentMode");
        static readonly bool TraceEnabled = Env.Var.Defined("NakeTraceEnabled");

        /// <summary>
        /// Prints trace-level message using specified format string and arguments. The message will be printed in DarkGreen color.
        /// The message will be printed only if Nake is called with --trace switch.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public static void TraceFormat(string message, params object[] args)
        {
            Trace(string.Format(message, args));
        }

        /// <summary>
        /// Prints trace-level message. The message will be printed in DarkGreen color.
        /// The message will be printed only if Nake is called with --trace switch.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Trace(string message)
        {
            if (!TraceEnabled)
                return;

            Color.With(ConsoleColor.DarkGreen, () => Out(message));
        }

        /// <summary>
        /// Prints simple message using specified format string and arguments. The message will be printed in DarkCyan color.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public static void MessageFormat(string message, params object[] args)
        {
            Message(string.Format(message, args));
        }

        /// <summary>
        /// Prints simple message. The message will be printed in DarkCyan color.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Message(string message)
        {
            if (SilentMode)
                return;

            Color.With(ConsoleColor.DarkCyan, () => Out(message));
        }

        /// <summary>
        /// Prints informational message using specified format string and arguments. The message will be printed in DarkGray color.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public static void InfoFormat(string message, params object[] args)
        {
            Info(string.Format(message, args));
        }

        /// <summary>
        /// Prints informational message. The message will be printed in DarkGray color.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Info(string message)
        {
            if (QuietMode)
                return;

            Color.With(ConsoleColor.DarkGray, () => Out(message));
        }

        /// <summary>
        /// Prints error message using specified exception as input. The message will be printed in DarkRed color.
        /// The stack trace will be printed only if Nake is called with --trace switch.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Error(Exception exception)
        {
            Error(exception.Message);

            if (TraceEnabled)
                Info(exception.StackTrace);
        }

        /// <summary>
        /// Prints error message. The message will be printed in DarkRed color.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Error(string message)
        {
            Color.With(ConsoleColor.DarkRed, () => Out(message));
        }
    }

    /// <summary>
    /// Collects all incoming calls for some action
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// Action<string> Log = (log) => Console.WriteLine(log);
    /// using (var logTrace = new Tracer(someAction))
    /// {
    ///     Log("Something important.");
    ///     var textWrittenToLog = logTrace.Trace;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class Tracer : IDisposable
    {
        public string Trace = string.Empty;

        // ReSharper disable once NotAccessedField.Local
        Action<string> initialRef;
        readonly Action<string> initialAction;

        internal Tracer(ref Action<string> initialRef)
        {
            this.initialRef = initialRef;
            initialAction = initialRef;
            initialRef = (l) =>
            {
                Record(l);
                initialAction(l);
            };
        }

        void Record(string log)
        {
            Trace += log + Environment.NewLine;
        }

        public void Dispose()
        {
            initialRef = initialAction;
        }
    }
}
