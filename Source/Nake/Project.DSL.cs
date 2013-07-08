using System;

namespace Nake
{
	public partial class Project
	{
		Scope currentNamespace = Scope.Root;
		string currentDescription;

		public void desc(string description)
		{
			if (String.IsNullOrWhiteSpace(description))
				throw new ArgumentException("Empty description is disallowed");

			if (currentDescription != null)
				throw new DuplicateDescriptionException();

			currentDescription = description;
		}

		public void @namespace(string name, Action define)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("name cannot be null or empty string");

			if (define == null)
				throw new ArgumentNullException("define");

			currentNamespace = currentNamespace.Push(name);

			define();

			currentNamespace = currentNamespace.Pop();
		}

		public Task task(string name, Action action)
		{
			return task(name, t => action());
		}

		public Task task(string name, Action<Task> action)
		{
			return DefineActionTask(name, new string[0], action);
		}

		public Task task(string name, string[] dependencies, Action action)
		{
			return task(name, dependencies, t => action());
		}

		public Task task(string name, string[] dependencies, Action<Task> action)
		{
			if (dependencies.Length == 0)
				throw new ArgumentException("Task dependencies resolve to an empty array");

			return DefineActionTask(name, dependencies, action);
		}

		public Task file(string name, Action action)
		{
			return file(name, t => action());
		}

		public Task file(string name, Action<FileTask> action)
		{
			return DefineFileTask(name, new string[0], action);
		}

		public Task file(string name, string[] dependencies, Action action)
		{
			return file(name, dependencies, t => action());
		}

		public Task file(string name, string[] dependencies, Action<FileTask> action)
		{
			if (dependencies.Length == 0)
				throw new ArgumentException("File dependencies resolve to an empty array");

			return DefineFileTask(name, dependencies, action);
		}

		public Task directory(string name)
		{
			return DefineDirectoryTask(name);
		}

		public string[] pre(params string[] dependencies)
		{
			return dependencies;
		}

		public void @default(string taskName)
		{
			task("default", new[] {taskName}, ()=> {});
		}
	}
}