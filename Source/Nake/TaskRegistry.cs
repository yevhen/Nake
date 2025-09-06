using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using AsyncTask = System.Threading.Tasks.Task;

namespace Nake;

public class TaskRegistry
{
    static string FullTypeName => typeof(TaskRegistry).FullName;

    internal static string BuildInvokeTaskString(string fullName, string arguments, string body) =>
        $@"{FullTypeName}.{nameof(InvokeTask)}(""{fullName}"", {arguments}, ()=> {body})";

    internal static string BuildInvokeTaskAsyncString(string fullName, string arguments, string body) => 
        $@"await {FullTypeName}.{nameof(InvokeTaskAsync)}(""{fullName}"", {arguments}, async ()=> {body})";

    internal static TaskRegistry Global;

    public static void InvokeTask(string fullName, TaskArgument[] arguments, Action body) =>
        Task(fullName).Invoke(arguments, body);

    public static async AsyncTask InvokeTaskAsync(string fullName, TaskArgument[] arguments, Func<AsyncTask> body) => 
        await Task(fullName).Invoke(arguments, body);

    public static async AsyncTask InvokeTask(string fullName, params TaskArgument[] arguments) => 
        await Task(fullName).Invoke(Global.script, arguments);

    readonly Dictionary<string, Task> tasks = new(new CaseInsensitiveEqualityComparer());
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
        var submission = assembly.GetType(Nake.Task.ScriptClass);

        var ctor = submission.GetConstructor([typeof(object[])]);
        Debug.Assert(ctor != null);

        var submissionStates = new object[2];
        submissionStates[0] = new object();

        var instance = ctor.Invoke([submissionStates]);
        // ReSharper disable once PossibleNullReferenceException
        submission.GetMethod("<Initialize>").Invoke(instance, []);

        return instance;
    }

    static Task Task(string fullName)
    {
        var task = Global.FindTask(fullName);
            
        if (task == null)
            throw new TaskNotFoundException(fullName);
            
        return task;
    }

    internal Task FindTask(string fullName) => tasks.Find(fullName);
    internal IEnumerable<Task> Tasks => tasks.Values;
}