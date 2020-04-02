using System;
using System.Diagnostics;

using Microsoft.Build.Utilities;

namespace Nake
{
    [Obsolete("Please use Session class")]
    public static class App
    {
        [Obsolete("Please use Session class")]
        public static void Exit(string message = null) => Session.Exit(message);

        [Obsolete("Please use Session class")]
        public static void Fail(string message = null) => Session.Fail(message);
    }

    [Obsolete("Please use Shell class")]
    public static class Run
    {
        [Obsolete("Please use Shell.Run")]
        public static int Cmd(
            string command,
            string[] environmentVariables = null,
            string workingDirectory = null,
            bool echoOff = true,
            bool ignoreStdOutErrors = false,
            bool ignoreExitCode = false,
            bool disableStdOutLogging = false)
        {
            var result = Shell.Run(command, environmentVariables, workingDirectory, echoOff, ignoreStdOutErrors, ignoreExitCode, quiet: disableStdOutLogging);
            return result.ExitCode;
        }

        [Obsolete("Unsupported. We recommend using functionality of MedallionShell package")]
        public static int Exec(
            string fileName, 
            string arguments, 
            string workingDirectory = null, 
            bool ignoreExitCode = false)
        {
            var info = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory,
                UseShellExecute = false,
            };

            using var process = new Process {StartInfo = info};

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0 && !ignoreExitCode)
                throw new ApplicationException($"Process exited with code {process.ExitCode}");

            return process.ExitCode;
        }

        [Obsolete("Just use as extension method")]
        public static TTask Exec<TTask>(
            TTask task, 
            bool ignoreStdOutErrors = true,
            bool disableStdOutLogging = false) where TTask : Task
        {
            task.Exec(ignoreStdOutErrors);
            return task;
        }
    }
}