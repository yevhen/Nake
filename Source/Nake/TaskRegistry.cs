using System;
using System.Collections.Generic;

namespace Nake
{
    public class TaskRegistry
    {
        public static void Invoke(string taskFullName, params TaskArgument[] arguments)
        {
            var task = Global.Find(taskFullName);

            if (task == null)
                throw new TaskNotFoundException(taskFullName);

            Invoker(task, arguments);
        }

        internal static TaskRegistry Global = new TaskRegistry();
        internal static Action<Task, TaskArgument[]> Invoker = (task, args) => task.Invoke(args);

        readonly Dictionary<string, Task> tasks = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());

        internal void Register(Task task)
        {
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
