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
            Build(new BuildOptions(null, path), code).Output;

        protected static string BuildFileNoRestoreCache(FileInfo path, string code) => 
            Build(new BuildOptions(null, path, false), code).Output;

        protected static CacheKey BuildFileWithCompilationCache(FileInfo path, string code) => 
            Build(new BuildOptions(null, path, true, true), code).Cached;

        protected static string Build(string code, Dictionary<string, string> substitutions) => 
            Build(new BuildOptions(substitutions), code).Output;

        protected static BuildEffects Build(BuildOptions options, string code)
        {
            var source = options.Source(code);

            var additionalReferences = new[]
            {
                new AssemblyReference(typeof(StepAttribute).Assembly.Location),
                new AssemblyReference(typeof(Env).Assembly.Location)
            };

            var output = new List<string>();
            void Logger(LogLevel level, string message, Exception exception)
            {
                output.Add(message);
                if (exception != null)
                    output.Add(exception.StackTrace);
            }

            var declarations = TaskDeclarationScanner.Scan(source);
            var builder = new BuildEngine(options.RestoreCache, Logger, additionalReferences);
            var engine = new CachingBuildEngine(builder, Task.From(declarations), !options.CompilationCache);
            var input = new BuildInput(source, options.Substitutions, false);
            
            var (result, cached) = engine.Build(input);
            TaskRegistry.Global = new TaskRegistry(result);

            return new BuildEffects(string.Join(Environment.NewLine, output), cached);
        }

        protected class BuildOptions
        {
            public readonly Dictionary<string, string> Substitutions;
            public readonly FileInfo Script;
            public readonly bool RestoreCache;
            public readonly bool CompilationCache;

            public BuildOptions(Dictionary<string, string> substitutions = null, FileInfo script = null, bool restoreCache = true, bool compilationCache = false)
            {
                Substitutions = substitutions ?? new Dictionary<string, string>();
                CompilationCache = compilationCache;
                Script = script;
                RestoreCache = restoreCache;
            }

            public ScriptSource Source(string code)
            {
                if (Script == null)
                    return new ScriptSource(code);

                Directory.CreateDirectory(Script.DirectoryName);
                File.WriteAllText(Script.FullName, code);

                return new ScriptSource(code, Script);
            }
        }

        protected class BuildEffects
        {
            public readonly string Output;
            public readonly CacheKey Cached;

            public BuildEffects(string output, CacheKey cached)
            {
                Output = output;
                Cached = cached;
            }
        }
    }
}
