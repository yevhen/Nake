using System;
using System.Linq;
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
            Assert.That(((string)result).Contains("Usage: dotnet tool"));

            result = Shell.Run(@"dotnet \
                                 tool \  
                                 list");

            Assert.That(result.ExitCode == 0);
            Assert.That(result.StdError.Count == 0);
            Assert.That(((string)result).Contains("---------------"));
        }
    }	
}