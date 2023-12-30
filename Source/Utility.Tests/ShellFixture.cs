using System;
using System.Linq;
using System.Threading.Tasks;

using Medallion.Shell;
using NUnit.Framework;
using NUnit.Framework.Legacy;

using static Nake.Shell;

namespace Nake.Utility	
{
    public class ShellFixture
    {
        class UsingCmd
        {
            [Test]
            public void Runs_via_shell_interpreter()
            {
                var result = Cmd("echo 42", captureOutput: true);
                Assert.That(result.StdOut[0], Is.EqualTo("42"));
            }

            [Test]
            public void Non_zero_exit_code() => Assert.Throws<ApplicationException>(() =>
                Cmd("foo blah", quiet: true));

            [Test]
            public void Ignore_exit_code()
            {
                var (exitCode, stdOut, stdError) = Cmd("foo blah", ignoreExitCode: true, quiet: true);
                Assert.That(exitCode != 0);
                Assert.That(stdError.Count > 0);
                Assert.That(stdOut.Count == 0);
            }

            [Test]
            public void Bash_style_line_continuations()
            {
                var result = Cmd(@"dotnet \
                                   tool --help", 
                                captureOutput: true);

                Assert.That(result.ExitCode == 0);
                Assert.That(result.StdError.Count == 0);
                Assert.That((string) result, Contains.Substring("dotnet tool [command] [options]"));

                result = Cmd(@"dotnet \
                                 tool \  
                                 list", 
                            captureOutput: true);

                Assert.That(result.ExitCode == 0);
                Assert.That(result.StdError.Count == 0);
                Assert.That((string) result, Contains.Substring("---------------"));
            }
        }

        class UsingRun
        {
            [Test]
            [TestCase("d", "d")]
            [TestCase("dotnet pack", "dotnet", "pack")]
            [TestCase("dotnet   pack", "dotnet", "pack")]
            [TestCase("dotnet   pack -c  Release", "dotnet", "pack", "-c", "Release")]
            [TestCase(@"dotnet 'C:\Program Files'", "dotnet", @"C:\Program Files")]
            [TestCase(@"dotnet 'C:\Program Files'  pack", "dotnet", @"C:\Program Files", "pack")]
            [TestCase(@"dotnet  '''C:\Program Files''' pack", "dotnet", @"'C:\Program Files'", "pack")]
            [TestCase(@"dotnet   '''C:\Program'' Files'''  pack", "dotnet", @"'C:\Program' Files'", "pack")]
            public async Task Command_line_splitting(string command, params string[] expected)
            {
                var echo = typeof(TestEcho.Program).Assembly.Location;
                echo = echo.Replace("Utility.Tests", "TestEcho");

                var tee = new Tee(Log.Out);
                var process = Run($"dotnet '{echo}' {command}").With(tee);

                var result = await process;
                Assert.That(result.Success);

                Assert.That(tee.StandardError().Count, Is.EqualTo(0));
                Assert.That(tee.StandardOutput().Count, Is.GreaterThan(0));
               
                var count = int.Parse(tee.StandardOutput().First());
                Assert.That(expected.Length, Is.EqualTo(count));
                CollectionAssert.AreEqual(expected, tee.StandardOutput().Skip(1).Take(count));
            }

            [Test]
            public void Unbalanced_quote()
            {
                const string command = @"dotnet '''C:\Program' Files'''  pack";
                var exception = Assert.Throws<Exception>(()=> ToCommandLineArgs(command));
                Assert.That(exception!.Message, Is.EqualTo("The command contains unbalanced quote at position 27"));
            }
        }
    }	
}