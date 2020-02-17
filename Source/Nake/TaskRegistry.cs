using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using AsyncTask = System.Threading.Tasks.Task;

namespace Nake
{
    public class TaskRegistry
    {
        internal static TaskRegistry Global;

        public static async AsyncTask Invoke(string taskFullName, params TaskArgument[] arguments)
        {
            var task = Global.Find(taskFullName);

            if (task == null)
                throw new TaskNotFoundException(taskFullName);

            await task.Invoke(Global.script, arguments);
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
            var submission = assembly.GetType(Task.ScriptClass);

            var ctor = submission.GetConstructor(new[] {typeof(object[])});
            Debug.Assert(ctor != null);

            var submissionStates = new object[2];
            submissionStates[0] = new object();

            var instance = ctor.Invoke(new object[] {submissionStates});
            submission.GetMethod("<Initialize>").Invoke(instance, new object[0]);

            return instance;
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
