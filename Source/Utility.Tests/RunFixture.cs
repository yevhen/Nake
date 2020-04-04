using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;	

namespace Nake.Utility	
{
    public class RunFixture
    {
        [Test]
        public async Task Invoking_via_await()
        {
            var result = await "echo 42";
            Assert.That(result.StdOut[0], Is.EqualTo("42"));
        }

        [Test]
        public void Non_zero_exit_code() => Assert.Throws<ApplicationException>(() =>
            Shell.Run("foo blah", quiet: true));

        [Test]
        public void Ignore_exit_code()
        {
            var result = Shell.Run("foo blah", ignoreExitCode: true, quiet: true);
            Assert.That(result.ExitCode != 0);
            Assert.That(result.StdError.Count > 0);
            Assert.That(result.StdOut.Count == 0);
        }

        [Test]
        public void Bash_style_line_continuations()
        {
            var result = Shell.Run(@"dotnet \
                                    tool --help");

            Assert.That(result.ExitCode == 0);
            Assert.That(result.StdError.Count == 0);
            Assert.That(((string) result).Contains("Usage: dotnet tool"));

            result = Shell.Run(@"dotnet \
                                 tool \  
                                 list");

            Assert.That(result.ExitCode == 0);
            Assert.That(result.StdError.Count == 0);
            Assert.That(((string) result).Contains("---------------"));
        }

        [Test]
        [TestCase("d", "d")]
        [TestCase("dotnet pack", "dotnet", "pack")]
        [TestCase("dotnet   pack", "dotnet", "pack")]
        [TestCase("dotnet   pack -c  Release", "dotnet", "pack", "-c", "Release")]
        [TestCase(@"dotnet 'C:\Program Files'", "dotnet", @"C:\Program Files")]
        [TestCase(@"dotnet 'C:\Program Files'  pack", "dotnet", @"C:\Program Files", "pack")]
        [TestCase(@"dotnet  '''C:\Program Files''' pack", "dotnet", @"'C:\Program Files'", "pack")]
        [TestCase(@"dotnet   '''C:\Program'' Files'''  pack", "dotnet", @"'C:\Program' Files'", "pack")]
        public void Command_line_splitting(string command, params string[] args)
        {
            CollectionAssert.AreEqual(args, ToCommandLineArgs(command));
        }

        [Test]
        public void Unbalanced_quote()
        {
            const string command = @"dotnet '''C:\Program' Files'''  pack";
            var exception = Assert.Throws<Exception>(()=> ToCommandLineArgs(command));
            Assert.AreEqual("The command contains unbalanced quote at position 27", exception.Message);
        }

        static string[] ToCommandLineArgs(string command)
        {
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