using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Medallion.Shell;

namespace Nake
{
    /// <summary>
    /// Shortcuts for running external programs or shell commands
    /// </summary>
    public static partial class Shell
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
        public static TaskAwaiter<CommandResult> GetAwaiter(this string command) => Run(command).Task.GetAwaiter();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="ignoreExitCode"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="quiet"></param>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        public static Command Run(
            string command,
            bool ignoreExitCode = false,
            string workingDirectory = null, 
            bool quiet = false,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("command is null or contains whitespace only");

            var args = ToCommandLineArgs(Prepare(command));
            if (args.Length == 0)
                throw new ArgumentException("command is null or contains whitespace only");

            var executable = args[0];
            var arguments = args.Skip(1);

            var result = Command.Run(executable, arguments, o => o
                .WorkingDirectory(workingDirectory ?? Location.CurrentDirectory)
                .EnvironmentVariables(environmentVariables ?? Env.Var)
                .ThrowOnError(!ignoreExitCode));

            if (!quiet)
                result.RedirectTo(Console.Out);

            return result;
        }

        internal static string[] ToCommandLineArgs(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return new string[0];

            var input = command.Trim();
            if (!input.Contains("'"))
                return input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var state = new ParserState(input);
            ParseArgument(state);

            return state.Arguments.ToArray();
        }

        static void ParseArgument(ParserState state)
        {
            switch (state.CurrentChar)
            {
                case '\'':
                    var start = state.Position;
                    state.Advance();
                    ParseInQuotes(state, start);
                    break;
                case ' ':
                    state.Advance();
                    state.Flush();
                    ParseWhitespace(state);
                    break;
                case ParserState.EndOfLine:
                    state.Flush();
                    break;
                default:
                    state.Append();
                    state.Advance();
                    ParseArgument(state);
                    break;
            }
        }

        static void ParseInQuotes(ParserState state, int start)
        {
            switch (state.CurrentChar)
            {
                case '\'':

                    var quotes = state.ConsecutiveQuotes();
                    state.Append('\'', quotes / 2);
                    state.Advance(quotes);

                    if (quotes % 2 == 0)
                    {
                        ParseInQuotes(state, start);
                        break;
                    }

                    ParseArgument(state);
                    break;

                case ParserState.EndOfLine:
                    throw new Exception($"The command contains unbalanced quote at position {start}");

                default:
                    state.Append();
                    state.Advance();
                    ParseInQuotes(state, start);
                    break;
            }
        }

        static void ParseWhitespace(ParserState ctx)
        {
            switch (ctx.CurrentChar)
            {
                case ' ': 
                    ctx.Advance();
                    ParseWhitespace(ctx);
                    break;
                default:
                    ParseArgument(ctx);
                    break;
            }
        }
        
        class ParserState
        {
            public const char EndOfLine = char.MinValue;

            readonly List<string> arguments = new List<string>();
            readonly StringBuilder buffer = new StringBuilder();
            
            readonly string input;

            int start;
            int length;
            
            public ParserState(string input)
            {
                this.input = input;
                CurrentChar = input[0];
            }

            public IEnumerable<string> Arguments => arguments;
            
            public int Position { get; private set; }
            public char CurrentChar { get; private set; }
            
            public void Advance(int count = 1)
            {
                Position += count;
                CurrentChar = Position < input.Length 
                    ? input[Position] 
                    : EndOfLine;
            }

            public void Append() => Append(CurrentChar);
            public void Append(char c, int count = 1)
            {
                while (count-- > 0)
                {
                    buffer.Append(c);
                    length += 1;
                }
            }

            public void Flush()
            {
                arguments.Add(buffer.ToString(start, length));
                start += length;
                length = 0;
            }
            
            public int ConsecutiveQuotes()
            {
                var count = 0;
                for (var i = Position; i < input.Length; i++)
                {
                    if (input[i] != '\'') break;
                    count++;
                }
                return count;
            }
        }
    }
}
