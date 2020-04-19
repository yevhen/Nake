using System;
using System.Collections.Generic;
using System.IO;

using Dotnet.Script.DependencyModel.Logging;

using NUnit.Framework;

namespace Nake
{
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
            Build(new BuildOptions(), code);
        
        protected static FileInfo BuildFile(string code)
        {
            var path = TempFilePath();
            Build(new BuildOptions(null, path), code);
            return path;
        }

        protected static string BuildFile(FileInfo path, string code) => 
            Build(new BuildOptions(null, path), code);

        protected static string BuildFileNoCache(FileInfo path, string code) => 
            Build(new BuildOptions(null, path, false), code);

        protected static string Build(string code, Dictionary<string, string> substitutions) => 
            Build(new BuildOptions(substitutions), code);

        protected static string Build(BuildOptions options, string code)
        {
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

            var engine = new Engine(options.Cache, Logger, additionalReferences);
            var source = new ScriptSource(code);

            if (options.Script != null)
            {
                Directory.CreateDirectory(options.Script.DirectoryName);
                File.WriteAllText(options.Script.FullName, code);
                source = new ScriptSource(code, options.Script);
            }

            var result = engine.Build(source, 
                options.Substitutions ?? new Dictionary<string, string>(), false);
            
            TaskRegistry.Global = new TaskRegistry(result);
            
            return string.Join(Environment.NewLine, output);
        }

        protected class BuildOptions
        {
            public readonly Dictionary<string, string> Substitutions;
            public readonly FileInfo Script;
            public readonly bool Cache;

            public BuildOptions(Dictionary<string, string> substitutions = null, FileInfo script = null, bool cache = true)
            {
                Substitutions = substitutions;
                Script = script;
                Cache = cache;
            }
        }
    }
}
