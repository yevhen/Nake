using System.Collections.Generic;
using NUnit.Framework;
using Nake.Scripting;

namespace Nake
{
    internal abstract class CodeFixture
    {
        [SetUp]
        public void SetUp()
        {
            TaskRegistry.Global = new TaskRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            TaskRegistry.Invoker = (task, args) => task.Invoke(args);
        }

        protected static void Invoke(string taskName, params TaskArgument[] args)
        {
            Find(taskName).Invoke(args);
        }

        protected static void Build(string code, Dictionary<string, string> substitutions = null)
        {
            var engine = new Engine();
            
            var output = engine.Build(
                code, substitutions ?? new Dictionary<string, string>(), false
            );

            foreach (var task in output.Tasks)
            {
                TaskRegistry.Global.Register(task);    
            }
        }

        protected static IEnumerable<Task> Tasks
        {
            get { return TaskRegistry.Global.Tasks; }
        }

        protected static Task Find(string taskName)
        {
            return TaskRegistry.Global.Find(taskName);
        }
    }
}
