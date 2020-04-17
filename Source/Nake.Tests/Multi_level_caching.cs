using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;
using NLog = Nake.Utility.Log;

namespace Nake
{
    [TestFixture]
    class Multi_level_caching : CodeFixture
    {
        [Test]
        public void Should_not_run_restore_when_no_dependencies_were_changed()
        {
            var path = Path.GetTempFileName();

            using var firstTime = new TraceStandardOutput();
            {
                Build(@"                

                    #r ""nuget: Streamstone, 2.3.0""
                    using Streamstone;

                    [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
                ",
                scriptFile: path);
            }

            Assert.That(firstTime.Output(), Contains.Substring("dotnet restore"));
            
            using var secondTime = new TraceStandardOutput();
            {
                Build(@"                

                    #r ""nuget: Streamstone, 2.3.0""
                    using Streamstone;

                    [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""B"", 10).ToString();
                ",
                scriptFile: path);
            }

            Assert.That(secondTime.Output(), !Contains.Substring("dotnet restore"));
        }

        class TraceStandardOutput : IDisposable
        {
            readonly Action<string> writer;
            readonly List<string> output = new List<string>();

            public TraceStandardOutput()
            {
                NLog.EnableTrace();
                writer = NLog.Out;
                NLog.Out = output.Add;
            }

            public void Dispose()
            {
                NLog.DisableTrace();
                NLog.Out = writer;
            }

            public string Output() => string.Join(Environment.NewLine, output);
        }
    }
}