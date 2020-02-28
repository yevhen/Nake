using System.Collections.Generic;

using NUnit.Framework;

namespace Nake
{
    abstract class CodeFixture
    {
        [SetUp]
        public void SetUp()
        {
            TaskRegistry.Global = new TaskRegistry();
        }

        protected static void Invoke(string taskName, params TaskArgument[] args) => 
            TaskRegistry.InvokeTask(taskName, args).GetAwaiter().GetResult();

        protected static void Build(string code, Dictionary<string, string> substitutions = null)
        {
            var engine = new Engine();
            
            var result = engine.Build(
                code, substitutions ?? new Dictionary<string, string>(), false
            );

            TaskRegistry.Global = new TaskRegistry(result);
        }

        protected static IEnumerable<Task> Tasks => TaskRegistry.Global.Tasks;
        protected static Task Find(string taskName) => TaskRegistry.Global.FindTask(taskName);
    }
}
