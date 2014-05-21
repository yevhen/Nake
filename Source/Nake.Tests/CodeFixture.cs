using System.Collections.Generic;

using Nake.Magic;
using Nake.Scripting;

using NUnit.Framework;

namespace Nake
{
    internal abstract class CodeFixture
    {
        [SetUp]
        public virtual void SetUp()
        {
            TaskRegistry.Global = new TaskRegistry();
        }

        [TearDown]
        public virtual void TearDown()
        {
            TaskRegistry.Invoker = (task, args) => task.Invoke(args);
        }

        protected void Invoke(string code, string taskName)
        {
            Build(code);

            Invoke(taskName);
        }

        protected void Invoke(string taskName, params TaskArgument[] args)
        {
            Find(taskName).Invoke(args);
        }

        protected void Build(string code, Dictionary<string, string> substitutions = null)
        {
            var script = Script.Build(code, substitutions ?? new Dictionary<string, string>(), false);

            foreach (var task in script.Tasks)
            {
                TaskRegistry.Global.Register(task);    
            }
        }

        protected IEnumerable<Task> Tasks
        {
            get { return TaskRegistry.Global.Tasks; }
        }

        protected Task Find(string taskName)
        {
            return TaskRegistry.Global.Find(taskName);
        }
    }
}
