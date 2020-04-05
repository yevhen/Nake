using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public static TaskAwaiter<Result> GetAwaiter(this string command) => GetAwaiter(Run(command));

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
        public static TaskAwaiter<Result> GetAwaiter(this Command command)
        {
            return RunTee(command).GetAwaiter();

            async Task<Result> RunTee(Command cmd)
            {
                var tee = new Tee(cmd, Log.Out);
                var result = await command.Task;
                return new Result(result.ExitCode, tee.StandardOutput(), tee.StandardError(), tee.MergedOutput());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="ignoreExitCode"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        public static Command Run(
            string command,
            bool ignoreExitCode = false,
            string workingDirectory = null, 
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
            
            return result;
        }

        class Tee
        {
            long seq; 

            readonly Action<string> onAddLine;
            readonly ConcurrentCollection standardOutput;
            readonly ConcurrentCollection standardError;

            public Tee(Command command, Action<string> onAddLine)
            {
                this.onAddLine = onAddLine;
                
                standardOutput = new ConcurrentCollection(this);
                standardError = new ConcurrentCollection(this);

                command.RedirectTo(standardOutput);
                command.RedirectStandardErrorTo(standardError);
            }

            public long Seq() => Interlocked.Increment(ref seq);

            public List<string> StandardOutput() => standardOutput.Items.Select(x => x.line).ToList();
            public List<string> StandardError() => standardError.Items.Select(x => x.line).ToList();

            public List<string> MergedOutput() => 
                standardOutput.Items
                    .Concat(standardError.Items)
                    .OrderBy(x => x.seq)
                    .Select(x => x.line)
                    .ToList();

            class ConcurrentCollection : ICollection<string>
            {
                readonly ConcurrentQueue<(long, string)> bag = new ConcurrentQueue<(long, string)>();

                readonly Tee tee;
                public ConcurrentCollection(Tee tee) => this.tee = tee;

                public void Add(string item)
                {
                    bag.Enqueue((tee.Seq(), item));
                    tee.onAddLine(item);
                }

                public IEnumerable<(long seq, string line)> Items => bag.ToArray();

                #region Unused
                public void Clear() => throw new NotImplementedException();
                public bool Contains(string item) => throw new NotImplementedException();
                public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
                public bool Remove(string item) => throw new NotImplementedException();
                public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                public int Count { get; }
                public bool IsReadOnly { get; }
                #endregion
            }
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
