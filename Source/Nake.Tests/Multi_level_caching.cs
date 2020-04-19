using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Multi_level_caching : CodeFixture
    {
        class Restore_dependencies
        {
            FileInfo path;

            [SetUp]
            public void SetUp() => path = TempFilePath();

            [Test]
            public void Does_not_run_restore_when_no_dependencies_were_changed()
            {
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
                var output = BuildFileNoRestoreCache(path, @"                

                    #r ""nuget: Streamstone, 2.3.0""
                    using Streamstone;

                    [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
                ");

                Assert.That(output, Contains.Substring("dotnet restore"));

                output = BuildFileNoRestoreCache(path, @"                

                    #r ""nuget: Streamstone, 2.3.0""
                    using Streamstone;

                    [Nake] void Test() => Env.Var[""ResolvedShard""] = Shard.Resolve(""B"", 10).ToString();
                ");

                Assert.That(output, Contains.Substring("dotnet restore"));
            }
        }

        class Code_changes
        {
            FileInfo path;

            [SetUp]
            public void SetUp()
            {
                path = TempFilePath();

                if (Directory.Exists(CacheKey.RootCacheFolder))
                    Directory.Delete(CacheKey.RootCacheFolder, true);
            }

            [Test]
            public void Does_not_recompile_when_no_code_changes()
            {
                var firstRun = BuildFileWithCompilationCache(path, @"                
                    [Nake] void Test(){}
                ");

                var assert = new CacheAssert(firstRun);

                var nextRun = BuildFileWithCompilationCache(path, @"                
                    [Nake] void Test(){}
                ");

                assert.SameCompilation(nextRun);
            }

            class CacheAssert
            {
                readonly CacheKey firstRun;
                readonly string[] firstRunFiles;
                readonly (string name, byte[] bytes)[] firstRunBytes;
                readonly (string name, DateTime modified)[] firstRunModified;

                public CacheAssert(CacheKey firstRun)
                {
                    this.firstRun = firstRun;

                    var compilations = Directory.GetDirectories(firstRun.ScriptFolder);
                    Assert.That(compilations.Length, Is.EqualTo(1));
                    Assert.That(compilations[0], Is.EqualTo(firstRun.CompilationFolder));

                    firstRunFiles = Directory.GetFiles(firstRun.CompilationFolder).OrderBy(x => x).ToArray();
                    firstRunBytes = firstRunFiles.Select(x => (name: x, bytes: File.ReadAllBytes(x))).ToArray();
                    firstRunModified = firstRunFiles.Select(x => (name: x, modified: File.GetLastWriteTime(x))).ToArray();
                }

                public void SameCompilation(CacheKey nextRun)
                {
                    var compilations = Directory.GetDirectories(nextRun.ScriptFolder);
                    Assert.That(nextRun.ScriptFolder, Is.EqualTo(firstRun.ScriptFolder), "It's the same script folder");
                    Assert.That(compilations.Length, Is.EqualTo(1), "Still a single compilation");
                    Assert.That(compilations[0], Is.EqualTo(firstRun.CompilationFolder), "Still same compilation key");

                    var nextRunFiles = Directory.GetFiles(nextRun.CompilationFolder).OrderBy(x => x).ToArray();
                    var nextRunBytes = nextRunFiles.Select(x => new {name = x, bytes = File.ReadAllBytes(x)}).ToArray();
                    var nextRunModified = nextRunFiles.Select(x => new {name = x, modified = File.GetLastWriteTime(x)}).ToArray();

                    CollectionAssert.AreEqual(firstRunFiles, nextRunFiles);

                    for (var i = 0; i < firstRunBytes.Length; i++)
                    {
                        var first = firstRunBytes[i];
                        var next = nextRunBytes[i];
                        CollectionAssert.AreEqual(first.bytes, next.bytes, 
                            $"Bytes are not equal between first and next run for {first.name}");    
                    }

                    for (var i = 0; i < firstRunModified.Length; i++)
                    {
                        var first = firstRunModified[i];
                        var next = nextRunModified[i];
                        Assert.AreEqual(first.modified, next.modified, 
                            $"Last write time is different between first and next run for {first.name}");    
                    }
                }
            }
        }
    }
}