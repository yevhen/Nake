using System;
using System.Collections.Generic;

namespace Nake
{
    public class TaskRegistry
    {
        internal static TaskRegistry Global;

        public static void Invoke(string taskFullName, params TaskArgument[] arguments)
        {
            var task = Global.Find(taskFullName);

            if (task == null)
                throw new TaskNotFoundException(taskFullName);

            task.Invoke(Global.script, arguments);
        }

        readonly Dictionary<string, Task> tasks = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());
        readonly object script;

        internal TaskRegistry()
        {}

        internal TaskRegistry(BuildResult result)
        {
            script = Activator.CreateInstance(
                result.Assembly.GetType(Task.ScriptClass), 
                new object[] {null, null}
            );

            foreach (var task in result.Tasks)
                tasks.Add(task.FullName, task);
        }

        internal Task Find(string taskFullName)
        {
            return tasks.Find(taskFullName);
        }
        
        internal IEnumerable<Task> Tasks
        {
            get { return tasks.Values; }
        }
    }
}
