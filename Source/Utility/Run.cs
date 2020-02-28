using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Build.Tasks;

namespace Nake
{
    /// <summary>
    /// Shortcuts for running external tools
    /// </summary>
    public static class Run
    {
        /// <summary>
        /// Executes specified command within a standard OS command-line interpreter using default options.
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>
        ///     <see cref="Result"/> object which could be further inspected
        ///     for exit code and std out and error messages</returns>
        /// <exception cref="ApplicationException">If command fails</exception>
        public static TaskAwaiter<Result> GetAwaiter(this string command) => Task.FromResult(Cmd(command)).GetAwaiter();

        /// <summary>
        /// Executes specified command within a standard OS command-line interpreter.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="environmentVariables">The environment variables pairs to pass. Default is all vars defined within a process</param>
        /// <param name="workingDirectory">The working directory. Default is current directory</param>
        /// <param name="echoOff">if set to <c>true</c>disables echoing command output to std out</param>
        /// <param name="ignoreStdError">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="ignoreExitCode">if set to <c>true</c> ignores exit code</param>
        /// <param name="quiet">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns>
        ///     <see cref="Result"/> object which could be further inspected
        ///     for exit code and std out and error messages</returns>
        /// <exception cref="ApplicationException">If command fails</exception>
        public static Result Cmd(
            string command, 
            string[] environmentVariables = null, 
            string workingDirectory = null, 
            bool echoOff = true,
            bool ignoreStdError = false, 
            bool ignoreExitCode = false,
            bool quiet = false)
        {
            var engine = new MSBuildEngineStub(quiet);

            var task = new Exec
            {   
                Command = command,
                EchoOff = echoOff,
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory(),
                IgnoreExitCode = ignoreExitCode,
                LogStandardErrorAsError = !ignoreStdError,
                EnvironmentVariables = environmentVariables ?? Env.Var.All(),
                ConsoleToMSBuild = true,
                BuildEngine = engine,
            };

            if (!task.Execute())
                throw new ApplicationException($"{task.GetType()} failed");

            var output = task.ConsoleOutput.Select(x => x.ItemSpec).ToList();
            return new Result(task.ExitCode, output, engine.StdError);
        }

        public class Result : IEnumerable<string>
        {
            public readonly int ExitCode;
            public readonly List<string> Out;
            public readonly List<string> Error;

            public Result(int exitCode, List<string> output, List<string> error)
            {
                ExitCode = exitCode;
                Error = error;
                var errIndex = new HashSet<string>(error);
                Out = output.Where(x => !errIndex.Contains(x)).ToList();
            }

            public IEnumerator<string> GetEnumerator() => Out.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Out).GetEnumerator();
        }
    }
}
