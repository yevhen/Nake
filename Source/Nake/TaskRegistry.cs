using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

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
            script = CreateScriptInstance(result.Assembly);

            foreach (var task in result.Tasks)
                tasks.Add(task.FullName, task);
        }

        static object CreateScriptInstance(Assembly assembly)
        {
            var ctor = assembly.GetType(Task.ScriptClass).GetConstructor(new[]
            {
                typeof(object[]),
                typeof(object).MakeByRefType()
            });

            Debug.Assert(ctor != null);

            var submissionStates = new object[2];
            submissionStates[0] = new object();

            return ctor.Invoke(new[] {submissionStates, new object()});
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
