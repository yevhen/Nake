using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Nake.Magic;

namespace Nake
{
    class Application
    {
        readonly Options options;
        
        public Application(Options options)
        {
            this.options = options;
        }

        public void Start()
        {
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

            Initialize();

            if (options.ShowTasks)
                ShowTasks();

            InvokeTasks();
        }

        void Initialize()
        {
            SetCurrentDirectory();
            OverrideEnvironmentVariables();
            
            var scriptFile = FindScriptFile();
            DefineNakeEnvironmentVariables(scriptFile);

            var compilation = BuildScript(scriptFile);
            RegisterTasks(compilation);
        }

        void SetCurrentDirectory()
        {
            var directory = options.CurrentDirectory ?? Environment.CurrentDirectory;
            Location.CurrentDirectory = () => directory;
        }

        void OverrideEnvironmentVariables()
        {
            foreach (var variable in options.Variables)
            {
                Env.Var[variable.Name] = variable.Value;
            }
        }

        static void DefineNakeEnvironmentVariables(FileInfo scriptFile)
        {
            Env.Var["NakeScriptDirectory"] = scriptFile.DirectoryName;
            Env.Var["NakeStartupDirectory"] = Location.CurrentDirectory();
        }

        FileInfo FindScriptFile()
        {
            if (options.ScriptFile != null)
            {
                var absoluteFilePath = !Path.IsPathRooted(options.ScriptFile) 
                    ? Path.GetFullPath(Path.Combine(Location.CurrentDirectory(), options.ScriptFile))
                    : options.ScriptFile;

                if (!File.Exists(absoluteFilePath))
                    throw new NakeException("Specified script file '{0}' doesn't exists", options.ScriptFile);

                return new FileInfo(absoluteFilePath);
            }

            var defaultScriptFile = Path.Combine(Location.CurrentDirectory(), "Nake.csx");

            if (!File.Exists(defaultScriptFile))
                throw new NakeException("Nake.csx file was not found in current directory [{0}]", Location.CurrentDirectory());

            return new FileInfo(defaultScriptFile);
        }

        Script BuildScript(FileInfo scriptFile)
        {
            return Script.Build(scriptFile, VariableSubstitutions(), options.DebugScript);
        }

        Dictionary<string, string> VariableSubstitutions()
        {
            return options.Variables.ToDictionary(x => x.Name, x => x.Value);
        }

        static void RegisterTasks(Script script)
        {
            var registry = TaskRegistry.Global;

            foreach (var task in script.Tasks)
            {
                registry.Register(task);
            }
        }

        public void ShowHelp()
        {
            Options.PrintUsage();

            Exit.Ok();
        }
        
        public void ShowVersion()
        {
            Log.Info(Assembly.GetExecutingAssembly().GetName().Version.ToString());

            Exit.Ok();
        }

        public void ShowTasks()
        {
            if (!TaskRegistry.Global.Tasks.Any())
            {
                Log.Info("Project defines 0 tasks");
                Exit.Ok();
            }

            var filter = options.ShowTasksFilter;

            var tasks = TaskRegistry.Global.Tasks.Where(x => x.Summary != "").ToArray();
            if (tasks.Length == 0)
                Exit.Ok();

            var maxTaskNameLength = tasks.Max(x => x.FullName.Length);

            tasks = tasks
                .OrderBy(x => x.FullName)
                .Where(x => filter == null || x.FullName.ToLower().Contains(filter.ToLower())).ToArray();

            Console.WriteLine();

            foreach (var task in tasks)
            {
                if (string.IsNullOrEmpty(task.Summary))
                    continue;

                Console.Write(Runner.Label(options.RunnerName) + " ");

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(task.FullName.ToLower().PadRight(maxTaskNameLength + 2));
                
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("# " + task.Summary);

                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine();
            Exit.Ok();
        }

        public void InvokeTasks()
        {
            var tasks = options.Tasks;

            if (tasks.Count == 0)
                tasks.Add(Options.Task.Default);

            foreach (var task in tasks)
            {
                var found = TaskRegistry.Global.Find(task.Name);

                if (found == null)
                    throw new TaskNotFoundException(task.Name);

                found.Invoke(task.Arguments);
            }
        }

        static void SetQuiet()
        {
            Env.Var["NakeQuietMode"] = "true";
        }

        static void SetSilent()
        {
            Env.Var["NakeSilentMode"] = "true";
        }

        static void SetTrace()
        {
            Env.Var["NakeTraceEnabled"] = "true";
        }
    }
}
