using NUnit.Framework;

namespace Nake.Utility
{
    public class RunFixture
    {
        [Test]
        public void Cmd_should_be_able_to_ignore_std_out_errors()
        {
            const string msg = "error: message";
            Assert.DoesNotThrow(() => Run.Cmd("Echo " + msg, ignoreStdOutErrors: true));
        }
    }
}