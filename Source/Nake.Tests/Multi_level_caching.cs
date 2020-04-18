using System.IO;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Multi_level_caching : CodeFixture
    {
        [Test]
        public void Should_not_run_restore_when_no_dependencies_were_changed()
        {
            var path = Path.GetTempFileName();

            var output = Build(@"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
            ",
            scriptFile: path);

            Assert.That(output, Contains.Substring("dotnet restore"));

            output = Build(@"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""B"", 10).ToString();
            ",
            scriptFile: path);

            Assert.That(output, !Contains.Substring("dotnet restore"));
        }
    }
}