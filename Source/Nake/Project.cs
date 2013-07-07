using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Nake
{
	partial class Project
	{
		readonly Dictionary<string, Task> tasks = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());

		public void Invoke(string name)
		{
			var task = LookupTaskByName(name) ?? LookTaskInRootScope(name);

			if (task == null)
				throw new TaskNotFoundException(name);

			task.Invoke();
		}

		Task DefineActionTask(string name, IEnumerable<string> prerequisites, Action<Task> action)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Task name cannot be null or empty string");

			if (name.Contains(":"))
				throw new ArgumentException("Task name cannot contain colons (:)");

			if (action == null)
				throw new ArgumentException("Task action cannot be null");

			var key = currentNamespace.TaskKey(name);
			
			if (tasks.ContainsKey(key))
				throw new DuplicateTaskException(key);

			return Define(new ActionTask(this, currentNamespace, name, action), key, prerequisites);
		}

		Task DefineFileTask(string name, IEnumerable<string> prerequisites, Action<FileTask> action)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("File name cannot be null or empty string");

			if (action == null)
				throw new ArgumentException("Task action cannot be null");

			if (tasks.ContainsKey(name))
				throw new DuplicateTaskException(name);

			return Define(new FileTask(this, currentNamespace, name, action), name, prerequisites);
		}

		Task DefineDirectoryTask(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Directory name cannot be null or empty string");

			if (tasks.ContainsKey(name))
				throw new DuplicateTaskException(name);

			return Define(new DirectoryTask(this, currentNamespace, name), name);
		}

		internal Task Define(Task task, string key, IEnumerable<string> prerequisites)
		{
			foreach (var prerequisite in prerequisites)
			{
				task.AddPrerequisite(prerequisite);
			}

			return Define(task, key);
		}

		internal Task Define(Task task, string key)
		{
			tasks.Add(key, task);

			task.Description = currentDescription ?? "";
			currentDescription = null;

			return task;
		}

		public IReadOnlyDictionary<string, Task> Tasks
		{
			get { return new ReadOnlyDictionary<string, Task>(tasks); }
		}

		internal Task Lookup(string name, Scope scope)
		{
			return (LookupTaskByName(name) ?? 
						(LookTaskInScope(name, scope) ??
							LookTaskInRootScope(name))) ?? 
								SynthesizeFileTask(name, scope);
		}

		Task LookTaskInRootScope(string name)
		{
			return LookTaskInScope(name, Scope.Root);
		}

		Task LookTaskInScope(string name, Scope scope)
		{
			return LookupTaskByName(scope.TaskKey(name));
		}

		Task LookupTaskByName(string name)
		{
			return tasks.Find(name);
		}

		Task SynthesizeFileTask(string name, Scope scope)
		{
			if (!IsValidFilePath(name) || !File.Exists(Location.GetFullPath(name)))
				return null;

			var task = new FileTask(this, scope, name, t => { });
			tasks.Add(name, task);
				
			return task;
		}

		static bool IsValidFilePath(string name)
		{
			return name.IndexOfAny(Path.GetInvalidPathChars()) == -1;
		}
	}
}
