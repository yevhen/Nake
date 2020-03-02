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
    /// Shortcuts for running external programs or shell commands
    /// </summary>
    public static class Shell
    {
        /// <summary>
        /// Runs the specified  program or command using default options.
        /// </summary>
        /// <param name="command">
        /// <para>
        /// The command(s) to run. These can be system commands, such as <c>attrib</c> (<c>chmod</c>),
        /// or an executable, such as <c>docker</c>, <c>run.bat</c>, or <c>setup.msi</c>.
        /// </para>
        /// <para>This parameter can contain multiple lines of commands.</para>
        /// </param>
        /// <returns>
        ///     <see cref="Result"/> object which could be further inspected
        ///     for exit code and std out and error messages</returns>
        /// <exception cref="ApplicationException">If command fails</exception>
        public static TaskAwaiter<Result> GetAwaiter(this string command) => Task.FromResult(Run(command)).GetAwaiter();

        /// <summary>
        /// Runs the specified program or command.
        /// </summary>
        /// <param name="command">
        /// <para>
        /// The command(s) to run. These can be system commands, such as <c>attrib</c> (<c>chmod</c>),
        /// or an executable, such as <c>docker</c>, <c>run.bat</c>, or <c>setup.msi</c>.
        /// </para>
        /// <para>This parameter can contain multiple lines of commands.</para>
        /// </param>
        /// <param name="environmentVariables">The environment variables pairs to pass. Default is all vars defined within a process</param>
        /// <param name="workingDirectory">The working directory. Default is current directory</param>
        /// <param name="echoOff">if set to <c>true</c>disables echoing command output to std out</param>
        /// <param name="ignoreStdError">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="ignoreExitCode">if set to <c>true</c> ignores exit code</param>
        /// <param name="useCommandProcessor">if set to <c>true</c> the command(s) will be executed via the batch file using the command-processor, rather than executed directly.</param>
        /// <param name="quiet">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns>
        ///     <see cref="Result"/> object which could be further inspected
        ///     for exit code and std out and error messages</returns>
        /// <exception cref="ApplicationException">If command fails</exception>
        public static Result Run(
            string command, 
            string[] environmentVariables = null, 
            string workingDirectory = null, 
            bool echoOff = true,
            bool ignoreStdError = false, 
            bool ignoreExitCode = false,
            bool useCommandProcessor = false,
            bool quiet = false)
        {
            var engine = new MSBuildEngineStub(quiet);

            var task = new Exec
            {   
                Command = command,
                EchoOff = echoOff,
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory,
                IgnoreExitCode = ignoreExitCode,
                LogStandardErrorAsError = !ignoreStdError,
                EnvironmentVariables = environmentVariables ?? Env.Var.All(),
                ConsoleToMSBuild = true,
                UseCommandProcessor = useCommandProcessor,
                BuildEngine = engine,
            };

            if (!task.Execute())
                throw new ApplicationException($"{task.GetType()} failed");

            var output = task.ConsoleOutput.Select(x => x.ItemSpec).ToList();
            return new Result(task.ExitCode, output, engine.StdError);
        }

        /// <summary>
        /// Represents the result of shell command or program execution.
        /// Contains properties to iterate over console output.
        /// </summary>
        public class Result : IEnumerable<string>
        {
            /// <summary>
            /// The exit code
            /// </summary>
            public readonly int ExitCode;

            /// <summary>
            /// The standard output
            /// </summary>
            public readonly List<string> StdOut;

            /// <summary>
            /// The standard error
            /// </summary>
            public readonly List<string> StdError;

            /// <summary>
            /// The console output (standard out + error)
            /// </summary>
            public readonly List<string> Output;

            internal Result(int exitCode, List<string> output, List<string> stdError)
            {
                ExitCode = exitCode;
                StdError = stdError;
                var errors = new HashSet<string>(stdError);
                StdOut = output.Where(x => !errors.Contains(x)).ToList();
                Output = output;
            }

            public IEnumerator<string> GetEnumerator() => Output.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Output).GetEnumerator();

            public static implicit operator string(Result r) => string.Join(Environment.NewLine, r.Output);
        }
    }
}
