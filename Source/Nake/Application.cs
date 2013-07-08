using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Roslyn.Compilers;

namespace Nake
{
	class Application
	{
		readonly Options options;
		readonly Project project;
		readonly ScriptingSession session;
		
		public Application(Options options)
		{
			this.options = options;

			project = new Project();
			session = new ScriptingSession(project);			
		}

		public void Start()
		{
			ShowHelp();
			ShowVersion();

			SetQuiet();
			SetSilent();
			SetTrace();

			Initialize();

			ShowTasks();
			InvokeTasks();
		}

		void Initialize()
		{
			SetCurrentDirectory();
			OverrideEnvironmentVariables();
			
			var projectFile = FindProjectFile();
			DefineNakeVariables(projectFile);
			
			LoadProject(projectFile);
			RedefinePassedVariables();
		}

		void SetCurrentDirectory()
		{
			var directory = options.CurrentDirectory ?? Environment.CurrentDirectory;
			Location.CurrentDirectory = () => directory;
		}

		void OverrideEnvironmentVariables()
		{
			var env = new Env();
			
			foreach (var property in options.Variables)
			{
				env[property.Key] = property.Value;
			}
		}

		void DefineNakeVariables(string projectFile)
		{
			DefineVariable("NakeProjectDirectory", Path.GetDirectoryName(projectFile));
			DefineVariable("NakeStartupDirectory", Location.CurrentDirectory());
		}

		void DefineVariable(string name, string value)
		{
			session.Execute(string.Format("public static string {0} = @\"{1}\";", name, value));
		}

		string FindProjectFile()
		{
			if (options.ProjectFile != null)
			{
				var absoluteFilePath = !Path.IsPathRooted(options.ProjectFile) 
					? Path.GetFullPath(Path.Combine(Location.CurrentDirectory(), options.ProjectFile))
					: options.ProjectFile;

				if (!File.Exists(absoluteFilePath))
					throw new NakeException("Specified project file '{0}' doesn't exists", options.ProjectFile);

				return absoluteFilePath;
			}

			var defaultProjectFile = Path.Combine(Location.CurrentDirectory(), "Nake.csx");

			if (!File.Exists(defaultProjectFile))
				throw new NakeException("Nake.csx file was not found in current directory [{0}]", Location.CurrentDirectory());

			return defaultProjectFile;
		}

		void LoadProject(string file)
		{
			session.Load(file);
		}

		void RedefinePassedVariables()
		{
			foreach (var property in options.Variables)
			{
				try
				{
					session.Execute(string.Format("{0} = \"{1}\";", property.Key, property.Value));
				}
				catch (CompilationErrorException e)
				{
					if (e.Message.Contains("CS0029"))
					{
						session.Execute(string.Format("{0} = {1};", property.Key, property.Value.ToLower()));
						return;
					}

					if (e.Message.Contains("CS0103"))
					{
						Out.TraceFormat("The project doesn't define variable {0}", property.Key);
						return;
					}

					throw;
				}
			}
		}

		public void ShowHelp()
		{
			if (!options.ShowHelp)
				return;
			
			Options.PrintUsage();

			Exit.Ok();
		}
		
		public void ShowVersion()
		{
			if (!options.ShowVersion)
				return;

			Out.Info(Assembly.GetExecutingAssembly().GetName().Version.ToString());

			Exit.Ok();
		}

		public void ShowTasks()
		{
			if (!options.ShowTasks)
				return;		

			if (!project.Tasks.Any())
			{
				Out.Info("Project defines 0 tasks");

				Exit.Ok();
			}

			var filter = options.ShowTasksFilter;

			var tasks = project.Tasks.Values.Where(x => x.Description != "").ToArray();
			if (tasks.Length == 0)
				Exit.Ok();

			var maxTaskNameLength = tasks.Max(x => x.DisplayName.Length);

			tasks = tasks
				.OrderBy(x => x.DisplayName)
				.Where(x => filter == null || x.DisplayName.ToLower().Contains(filter.ToLower())).ToArray();

			Console.WriteLine();

			foreach (var task in tasks)
			{
				if (string.IsNullOrEmpty(task.Description))
					continue;

				Console.Write("nake ");

				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.Write(task.DisplayName.PadRight(maxTaskNameLength + 2));
				
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.Write("# " + task.Description);

				Console.ResetColor();
				Console.WriteLine();
			}

			Console.WriteLine();

			Exit.Ok();
		}

		public void InvokeTasks()
		{
			var tasks = options.Tasks.ToList();

			if (tasks.Count == 0)
				tasks.Add("default");

			foreach (var task in tasks)
			{
				project.Invoke(task);
			}
		}

		void SetQuiet()
		{
			Out.QuietMode = options.QuietMode;
		}

		void SetSilent()
		{
			Out.SilentMode = options.SilentMode;
		}

		void SetTrace()
		{
			Out.TraceEnabled = options.TraceEnabled;
		}
	}
}
