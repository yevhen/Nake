using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Nake.Magic;
using Nake.Scripting;

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

            var scriptFile = FindScriptFile();
            var preprocessed = Preprocess(scriptFile);
            
            var code = preprocessed.Code();
            var declarations = ScanTaskDeclarations(code);

            if (options.ShowTasks)
                ShowTasks(declarations);

            OverrideEnvironmentVariables();
            DefineNakeEnvironmentVariables(scriptFile);

            var tasks = Build(scriptFile, preprocessed, code, declarations);
            Register(tasks);

            Invoke();
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

        IEnumerable<Task> Build(FileInfo file, PreprocessorResult script, string code, IEnumerable<TaskDeclaration> declarations)
        {
            var engine = new Engine(
                script.References, 
                script.AbsoluteReferences, 
                script.Namespaces
            );

            var cachingEngine = new CachingEngine(
                engine, file, declarations.Select(x => new Task(x)).ToArray()              
            );

            var output = cachingEngine.Build(
                code, VariableSubstitutions(), options.DebugScript
            );

            return output.Tasks;
        }

        Dictionary<string, string> VariableSubstitutions()
        {
            return options.Variables.ToDictionary(x => x.Name, x => x.Value);
        }

        static void Register(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
                TaskRegistry.Global.Register(task);
        }

        static void ShowHelp()
        {
            Options.PrintUsage();

            Exit.Ok();
        }

        static void ShowVersion()
        {
            Log.Info(Assembly.GetExecutingAssembly().GetName().Version.ToString());

            Exit.Ok();
        }

        void ShowTasks(TaskDeclaration[] tasks)
        {
            if (!tasks.Any())
            {
                Log.Info("Project defines 0 tasks");
                Exit.Ok();
            }

            var filter = options.ShowTasksFilter;
            var breadth = tasks.Max(x => x.FullName.Length);

            tasks = tasks
                .OrderBy(x => x.FullName)
                .Where(x => filter == null || x.FullName.ToLower().Contains(filter.ToLower())).ToArray();

            Console.WriteLine();

            var defaultTask = tasks.SingleOrDefault(x => x.FullName.ToLower() == "default");
            if (defaultTask != null)
                PrintTask(defaultTask, breadth, ConsoleColor.Cyan);

            foreach (var task in tasks.Where(x => x.FullName.ToLower() != "default"))
                PrintTask(task, breadth);

            Console.WriteLine();
            Exit.Ok();
        }

        void PrintTask(TaskDeclaration task, int breadth, ConsoleColor color = ConsoleColor.DarkGreen)
        {
            Console.Write(Runner.Label(options.RunnerName) + " ");

            Console.ForegroundColor = color;
            Console.Write(task.FullName.ToLower().PadRight(breadth + 2));

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("# " + task.Summary);

            Console.ResetColor();
            Console.WriteLine();
        }

        TaskDeclaration[] ScanTaskDeclarations(string code)
        {
            return new TaskDeclarationScanner().Scan(code, options.ShowTasks).ToArray();
        }

        static PreprocessorResult Preprocess(FileInfo scriptFile)
        {
            return new Preprocessor().Process(scriptFile);
        }

        void Invoke()
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
