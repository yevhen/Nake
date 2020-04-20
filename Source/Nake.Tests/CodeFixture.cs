using System;
using System.Collections.Generic;
using System.IO;

using Dotnet.Script.DependencyModel.Logging;
using NUnit.Framework;

namespace Nake
{
    using Magic;
    using Scripting;

    abstract class CodeFixture
    {
        [SetUp]
        public void SetUp()
        {
            TaskRegistry.Global = new TaskRegistry();
        }

        protected static void Invoke(string taskName, params TaskArgument[] args) => 
            TaskRegistry.InvokeTask(taskName, args).GetAwaiter().GetResult();

        protected static IEnumerable<Task> Tasks => TaskRegistry.Global.Tasks;
        protected static Task Find(string taskName) => TaskRegistry.Global.FindTask(taskName);

        protected static FileInfo TempFilePath()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            return new FileInfo(Path.Combine(dir, $"{Guid.NewGuid():N}.tmp"));
        }

        protected static string Build(string code) => 
            Build(new BuildOptions(), code).Output;
        
        protected static FileInfo BuildFile(string code)
        {
            var path = TempFilePath();
            Build(new BuildOptions(null, path), code);
            return path;
        }

        protected static string BuildFile(FileInfo path, string code) => 
            Build(new BuildOptions(null, path, true), code).Output;

        protected static BuildEffects BuildFileWithCompilationCache(FileInfo path, string code) => 
            Build(new BuildOptions(null, path), code);

        protected static string Build(string code, Dictionary<string, string> substitutions) => 
            Build(new BuildOptions(substitutions), code).Output;

        protected static BuildEffects Build(BuildOptions options, string code)
        {
            var output = new List<string>();

            void Logger(LogLevel level, string message, Exception exception)
            {
                output.Add(message);
                if (exception != null)
                    output.Add(exception.StackTrace);
            }

            var source = options.Source(code, Logger);

            var additionalReferences = new[]
            {
                new AssemblyReference(typeof(StepAttribute).Assembly.Location),
                new AssemblyReference(typeof(Env).Assembly.Location)
            };

            var declarations = TaskDeclarationScanner.Scan(source);
            var builder = new BuildEngine(additionalReferences);
            var engine = new CachingBuildEngine(builder, Task.From(declarations), options.ResetCache);
            var input = new BuildInput(source, options.Substitutions, false);
            
            var (result, cached) = engine.Build(input);
            TaskRegistry.Global = new TaskRegistry(result);

            return new BuildEffects(string.Join(Environment.NewLine, output), cached);
        }

        protected class BuildOptions
        {
            public readonly Dictionary<string, string> Substitutions;
            public readonly FileInfo Script;
            public readonly bool ResetCache;

            public BuildOptions(Dictionary<string, string> substitutions = null, FileInfo script = null, bool resetCache = false)
            {
                Substitutions = substitutions ?? new Dictionary<string, string>();
                Script = script;
                ResetCache = resetCache;
            }

            public ScriptSource Source(string code, Logger log = null)
            {
                if (Script == null)
                    return new ScriptSource(code);

                Directory.CreateDirectory(Script.DirectoryName);
                File.WriteAllText(Script.FullName, code);

                return new ScriptSource(code, Script, log);
            }
        }

        protected class BuildEffects
        {
            public void Deconstruct(out string output, out CacheKey cache)
            {
                output = Output;
                cache = Cache;
            }

            public readonly string Output;
            public readonly CacheKey Cache;

            public BuildEffects(string output, CacheKey cache)
            {
                Output = output;
                Cache = cache;
            }
        }
    }
}
