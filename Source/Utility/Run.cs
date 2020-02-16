using System;
using System.Diagnostics;

using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace Nake
{
    /// <summary>
    /// Shortcuts for running external tools
    /// </summary>
    public static class Run
    {
        /// <summary>
        /// Executes specified command within a standard OS command-line interpreter.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="environmentVariables">The environment variables pairs to pass. Default is all vars defined within a process</param>
        /// <param name="workingDirectory">The working directory. Default is current directory</param>
        /// <param name="echoOff">if set to <c>true</c>disables echoing command output to std out</param>
        /// <param name="ignoreStdOutErrors">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="ignoreExitCode">if set to <c>true</c> ignores exit code</param>
        /// <param name="disableStdOutLogging">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns> Exit code </returns>
        /// <exception cref="System.ApplicationException">If command fails</exception>
        public static int Cmd(
            string command, 
            string[] environmentVariables = null, 
            string workingDirectory = null, 
            bool echoOff = true,
            bool ignoreStdOutErrors = false, 
            bool ignoreExitCode = false,
            bool disableStdOutLogging = false)
        {
            var task = new Exec
            {   
                Command = command,
                EchoOff = echoOff,
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory(),
                LogStandardErrorAsError = !ignoreStdOutErrors,
                IgnoreStandardErrorWarningFormat = ignoreStdOutErrors,
                IgnoreExitCode = ignoreExitCode,
                EnvironmentVariables = environmentVariables ?? Env.Var.All(),
                BuildEngine = new MSBuildEngineStub(disableStdOutLogging),
            };

            if (!task.Execute() || task.Log.HasLoggedErrors)
                throw new ApplicationException($"{task.GetType()} failed");
            
            return task.ExitCode;
        }

        /// <summary>
        /// Executes the specified file with given argument string.
        /// </summary>
        /// <param name="fileName">Name of the executable file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDirectory">The working directory. Default is current directory</param>
        /// <param name="ignoreExitCode">if set to <c>true</c> ignores exit code</param>
        /// <returns> Exit code </returns>
        public static int Exec(
            string fileName, 
            string arguments, 
            string workingDirectory = null, 
            bool ignoreExitCode = false)
        {
            var info = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory(),
                UseShellExecute = false,
            };

            using (var process = new Process())
            {
                process.StartInfo = info;
                
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0 && !ignoreExitCode)
                    throw new ApplicationException(string.Format("Process exited with code {0}", process.ExitCode));

                return process.ExitCode;
            }
        }

        /// <summary>
        /// Executes MSBuild task.
        /// </summary>
        /// <typeparam name="TTask">The type of the task.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="ignoreStdOutErrors">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="disableStdOutLogging">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns>The executed task. Allows to get value of any OUT property</returns>
        /// <exception cref="System.ApplicationException">If tasks fails</exception>
        public static TTask Exec<TTask>(
            TTask task, 
            bool ignoreStdOutErrors = true,
            bool disableStdOutLogging = false) where TTask : Task
        {
            task.BuildEngine = new MSBuildEngineStub(disableStdOutLogging);

            if (!task.Execute() || (task.Log.HasLoggedErrors && !ignoreStdOutErrors))
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));

            return task;
        }
    }

    /// <summary>
    /// Funky way to run a cli command
    /// </summary>
    public static class WagTheTail
    {
        /// <summary>
        /// Runs cli command
        /// </summary>
        /// <param name="command">The command line to pass to <see cref="Run.Cmd"/></param>
        public static void _(this string command) => Run.Cmd(command);
    }
}
