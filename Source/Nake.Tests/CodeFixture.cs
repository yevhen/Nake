using System.Collections.Generic;
using System.IO;
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

        protected static void Build(string code, Dictionary<string, string> substitutions = null, string scriptFile = null)
        {
            var additionalReferences = new[]
            {
                new AssemblyReference(typeof(StepAttribute).Assembly.Location),
                new AssemblyReference(typeof(Env).Assembly.Location)
            };

            var engine = new Engine(additionalReferences);
            var source = new ScriptSource(code);

            if (scriptFile != null)
            {
                File.WriteAllText(scriptFile, code);
                source = new ScriptSource(code, new FileInfo(scriptFile));
            }

            var result = engine.Build(source, 
                substitutions ?? new Dictionary<string, string>(), false);
            
            TaskRegistry.Global = new TaskRegistry(result);
        }

        protected static IEnumerable<Task> Tasks => TaskRegistry.Global.Tasks;
        protected static Task Find(string taskName) => TaskRegistry.Global.FindTask(taskName);
    }
}
