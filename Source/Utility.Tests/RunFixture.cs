using System;
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
            Assert.That(result.Out[0], Is.EqualTo("42"));
        }

        [Test]	
        public void Non_zero_exit_code() => Assert.Throws<ApplicationException>(() => 
            Run.Cmd("foo blah", quiet: true));

        [Test]	
        public void Ignore_exit_code()	
        {	
            var result = Run.Cmd("foo blah", ignoreExitCode: true, quiet: true);
            Assert.That(result.ExitCode != 0);
            Assert.That(result.Error.Count > 0);
            Assert.That(result.Out.Count == 0);
        }
    }	
}