using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Nake.Magic;
using Nake.Scripting;
using Nake.Utility;
using static System.Environment;
using AsyncTask = System.Threading.Tasks.Task;

namespace Nake;

class Application
{
    string currentDirectory = Environment.CurrentDirectory;
    readonly Options options;
        
    public Application(Options options) => 
        this.options = options;

    public async AsyncTask Start()
    {
        SetCurrentDirectory();

        if (options.ShowHelp)
            ShowHelp();

        if (options.ShowVersion)
            ShowVersion();
            
        if (options.QuietMode)
            SetQuiet();

        if (options.SilentMode)
            SetSilent();

        if (options.TraceEnabled)
            SetTrace();

        var file = Find();
        var source = Parse(file);
        var declarations = Scan(source);

        if (options.ShowTasks)
            ShowTasks(declarations);

        OverrideEnvironmentVariables();
        DefineNakeEnvironmentVariables();

        await Invoke(source, declarations);
    }

    void SetCurrentDirectory()
    {
        var directory = options.CurrentDirectory ?? CurrentDirectory;
        currentDirectory = directory;
    }

    void OverrideEnvironmentVariables()
    {
        foreach (var variable in options.Variables)
            SetEnvironmentVariable(variable.Name, variable.Value);
    }

    void DefineNakeEnvironmentVariables()
    {
        SetEnvironmentVariable("NakeStartupDirectory", currentDirectory);
        SetEnvironmentVariable("NakeWorkingDirectory", CurrentDirectory);
    }

    FileInfo Find()
    {
        if (options.ScriptFile != null)
        {
            var absoluteFilePath = !Path.IsPathRooted(options.ScriptFile) 
                ? Path.GetFullPath(Path.Combine(currentDirectory, options.ScriptFile))
                : options.ScriptFile;

            if (!File.Exists(absoluteFilePath))
                throw new NakeException("Specified script file '{0}' doesn't exists", options.ScriptFile);

            return new FileInfo(absoluteFilePath);
        }

        var defaultScriptFile = Path.Combine(currentDirectory, "Nake.csx");

        if (!File.Exists(defaultScriptFile))
            throw new NakeException("Nake.csx file was not found in current directory [{0}]", currentDirectory);

        return new FileInfo(defaultScriptFile);
    }

    BuildResult Build(ScriptSource source, IEnumerable<TaskDeclaration> declarations)
    {
        var input = new BuildInput(source, VariableSubstitutions(), options.DebugScript);

        var engine = new CachingBuildEngine(
            new BuildEngine(), 
            Task.From(declarations), 
            options.ResetCache);

        return engine.Build(input).result;
    }

    Dictionary<string, string> VariableSubstitutions() => 
        options.Variables.ToDictionary(x => x.Name, x => x.Value);

    static void Initialize(BuildResult result) => 
        TaskRegistry.Global = new TaskRegistry(result);

    static void ShowHelp()
    {
        Options.PrintUsage();

        Session.Exit();
    }

    static void ShowVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        Debug.Assert(version != null, nameof(version) + " != null");
        Log.Info(version.InformationalVersion);

        Session.Exit(); ;
    }

    void ShowTasks(TaskDeclaration[] tasks)
    {
        if (!tasks.Any())
        {
            Log.Info("Project defines 0 tasks");
            Session.Exit();
        }

        var filter = options.ShowTasksFilter;
        var breadth = tasks.Max(x => x.DisplayName.Length);

        tasks = tasks
            .OrderBy(x => x.DisplayName)
            .Where(x => filter == null || 
                        x.DisplayName.Contains(filter.ToLower()) || 
                        x.Summary.Contains(filter.ToLower()))
            .ToArray();

        Console.WriteLine();

        var @default = tasks.SingleOrDefault(x => x.DisplayName == "default");
        if (@default is not null)
            PrintTask(@default, breadth, ConsoleColor.Cyan);

        foreach (var task in tasks.Where(x => x.DisplayName != "default"))
            PrintTask(task, breadth);

        Console.WriteLine();
        Session.Exit();
    }

    void PrintTask(TaskDeclaration task, int breadth, ConsoleColor color = ConsoleColor.DarkGreen)
    {
        Console.Write(Runner.Label(options.RunnerName) + " ");

        Console.ForegroundColor = color;
        Console.Write(task.DisplayName.PadRight(breadth + 2));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("# " + task.Summary);

        Console.ResetColor();
        Console.WriteLine();
    }

    ScriptSource Parse(FileInfo file) => new(File.ReadAllText(file.FullName), file, null, options.Framework);
    static TaskDeclaration[] Scan(ScriptSource source) => TaskDeclarationScanner.Scan(source);

    async AsyncTask Invoke(ScriptSource source, IEnumerable<TaskDeclaration> declarations)
    {
        var result = Build(source, declarations);
        Initialize(result);

        var tasks = options.Tasks;
        if (tasks.Count == 0)
            tasks.Add(Options.Task.Default);

        foreach (var task in tasks)
            await TaskRegistry.InvokeTask(task.Name, task.Arguments);
    }
        
    static void SetQuiet() => SetEnvironmentVariable("NakeQuietMode", "true");
    static void SetSilent() => SetEnvironmentVariable("NakeSilentMode", "true");
    static void SetTrace() => SetEnvironmentVariable("NakeTraceEnabled", "true");
}