using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Multi_level_caching : CodeFixture
    {
        [Test]
        public void Does_not_run_restore_when_no_dependencies_were_changed()
        {
            var path = TempFilePath();

            var output = BuildFile(path, @"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
            ");

            Assert.That(output, Contains.Substring("dotnet restore"));

            output = BuildFile(path, @"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""B"", 10).ToString();
            ");

            Assert.That(output, !Contains.Substring("dotnet restore"));
        }

        [Test]
        public void Runs_restore_when_cache_disabled_even_when_no_dependencies_were_changed()
        {
            var path = TempFilePath();

            var output = BuildFileNoCache(path, @"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
            ");

            Assert.That(output, Contains.Substring("dotnet restore"));

            output = BuildFileNoCache(path, @"                

                #r ""nuget: Streamstone, 2.3.0""
                using Streamstone;

                [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""B"", 10).ToString();
            ");

            Assert.That(output, Contains.Substring("dotnet restore"));
        }
    }
}