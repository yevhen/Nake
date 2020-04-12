using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Build.Tasks;

namespace Nake
{
    /// <summary>
    /// Shortcuts for running external programs or shell commands
    /// </summary>
    public static partial class Shell
    {
        /// <summary>
        /// Runs the specified program or command using OS command interpreter.
        /// For linux it wil be run via <c>"/bin/bash -c"</c> on Windows via <c>cmd</c>
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
        /// <param name="captureOutput">set to <c>true</c> to capture the standard error and output</param>
        /// <param name="useCommandProcessor">if set to <c>true</c> the command(s) will be executed via the batch file using the command-processor, rather than executed directly.</param>
        /// <param name="quiet">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns>
        ///     <see cref="Result"/> object which could be further inspected
        ///     for exit code and std out and error messages, if captured</returns>
        /// <exception cref="ApplicationException">If command fails</exception>
        public static Result Cmd(
            string command, 
            string[] environmentVariables = null, 
            string workingDirectory = null, 
            bool echoOff = true,
            bool ignoreStdError = false, 
            bool ignoreExitCode = false,
            bool captureOutput = false,
            bool useCommandProcessor = false,
            bool quiet = false)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("command is null or contains whitespace only");

            var engine = new MSBuildEngineStub(quiet);

            var task = new Exec
            {   
                Command = Prepare(command),
                EchoOff = echoOff,
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory,
                IgnoreExitCode = ignoreExitCode,
                LogStandardErrorAsError = !ignoreStdError,
                EnvironmentVariables = environmentVariables ?? Env.Var.All(),
                ConsoleToMSBuild = captureOutput,
                UseCommandProcessor = useCommandProcessor,
                BuildEngine = engine,
            };

            if (!task.Execute())
                throw new ApplicationException($"{task.GetType()} failed");

            var output = task.ConsoleOutput.Select(x => x.ItemSpec).ToList();

            // HACK: I can't find way for now to get error output cleanly separated
            var errors = new HashSet<string>(engine.StdError);
            var stdOut = output.Where(x => !errors.Contains(x)).ToList();

            return new Result(task.ExitCode, stdOut, engine.StdError, output);
        }

        /// <summary>
        /// Represents the result of shell command or program execution.
        /// Contains properties to iterate over console output.
        /// </summary>
        public class Result : IEnumerable<string>
        {
            /// <summary>
            /// Returns true ff the exit code is 0 (indicating success)
            /// </summary>
            public bool Success => ExitCode == 0;

            /// <summary>
            /// The exit code
            /// </summary>
            public readonly int ExitCode;

            /// <summary>
            /// The standard output, if captured
            /// </summary>
            public readonly List<string> StdOut;

            /// <summary>
            /// The standard error, if captured
            /// </summary>
            public readonly List<string> StdError;

            /// <summary>
            /// The merged console output (standard out + error), if captured 
            /// </summary>
            public readonly List<string> Output;

            internal Result(int exitCode, List<string> stdOut, List<string> stdError, List<string> output)
            {
                ExitCode = exitCode;
                StdOut = stdOut;
                StdError = stdError;
                Output = output;
            }

            public IEnumerator<string> GetEnumerator() => Output.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Output).GetEnumerator();

            public static implicit operator string(Result r) => r.ToString();
            public override string ToString() => string.Join(Environment.NewLine, Output);

            /// <summary>
            /// Helper method to deconstruct into a tuple of ExitCode and Output
            /// </summary>
            /// <param name="exitCode"><see cref="ExitCode"/></param>
            /// <param name="output"><see cref="Output"/> as a string</param>
            public void Deconstruct(out int exitCode, out string output)
            {
                exitCode = ExitCode;
                output = ToString();
            }

            /// <summary>
            /// Helper method to deconstruct into a tuple of ExitCode, StdOut and StdError
            /// </summary>
            /// <param name="exitCode"><see cref="ExitCode"/></param>
            /// <param name="stdOut"><see cref="StdOut"/></param>
            /// <param name="stdError"><see cref="StdError"/></param>
            public void Deconstruct(out int exitCode, out List<string> stdOut, out List<string> stdError)
            {
                exitCode = ExitCode;
                stdOut = StdOut;
                stdError = StdError;
            }
        }

        static string Prepare(string command)
        {
            var lines = command.Split("\n");
            return lines.Length == 1 ? command : PrepareMultiline(Line.From(lines));
        }

        static string PrepareMultiline(Line[] lines)
        {
            var result = new StringBuilder();
            Array.ForEach(lines, each => each.Append(result));
            return result.ToString();
        }

        class Line
        {
            public static Line[] From(IEnumerable<string> lines) => lines.Aggregate(new List<Line>(), (list, line) =>
            {
                list.Add(new Line(line, list.LastOrDefault()));
                return list;
            })
            .ToArray();

            readonly bool continues;
            readonly string value;

            Line(string line, Line previous = null)
            {
                var trimmed = line.TrimEnd();
                continues = trimmed.EndsWith(" \\");
                
                value = continues 
                    ? trimmed.Substring(0, trimmed.Length - 1) 
                    : $"{line}{Environment.NewLine}";

                if (previous?.continues == true)
                    value = value.TrimStart();
            }

            public void Append(StringBuilder result) => result.Append(value);
        }
    }
}
