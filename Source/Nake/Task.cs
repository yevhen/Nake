using System;
using System.Collections.Generic;
using System.Linq;

namespace Nake
{
	public abstract class Task
	{
		readonly List<string> prerequisites = new List<string>();

		readonly Project project;
		readonly string name;
		readonly Scope scope;
		
		bool invoked;

		internal Task(Project project, Scope scope, string name)
		{
			this.project = project;
			this.scope = scope;
			this.name = name;

			Description = "";
		}

		public virtual string Key
		{
			get { return name; }
		}

		protected string ScopedTaskKey()
		{
			return scope.TaskKey(name);
		}

		public virtual string DisplayName
		{
			get { return name; }
		}

		protected string ScopedTaskDisplayName()
		{
			return scope.TaskDisplayName(name);
		}

		public string Description
		{
			get; set;
		}

		public IEnumerable<string> Prerequisites
		{
			get { return prerequisites; }
		}

		public IEnumerable<Task> PrerequisiteTasks()
		{
			return Prerequisites.Select(LookupPrerequisite);
		}

		Task LookupPrerequisite(string prerequisite)
		{
			var task = project.Lookup(prerequisite, scope);

			if (task == null)
				throw new TaskPrerequisiteNotFoundException(this, prerequisite);

			return task;
		}

		public void Invoke()
		{
			Invoke(InvocationChain.Start);
		}

		void Invoke(InvocationChain chain)
		{
			chain = chain.Append(this);

			Out.TraceFormat("** Invoke '{0}' {1}", DisplayName, InvocationStatus());

			if (invoked)
				return;

			invoked = true;
			InvokePrerequisites(chain);

			if (IsNeeded())
				Execute();
		}

		string InvocationStatus()
		{
			var state = new List<string>();

			if (!invoked)
				state.Add("first time");

			if (!IsNeeded())
				state.Add("not needed");

			return state.Any() ? string.Join(", ",  state) : "";
		}

		void InvokePrerequisites(InvocationChain chain)
		{
			foreach (var prerequisite in PrerequisiteTasks())
			{
				prerequisite.Invoke(chain);
			}
		}

		public void Execute()
		{
			Out.TraceFormat("** Execute '{0}'", name);

			DoExecute();
		}

		public void Reenable()
		{
			invoked = false;
		}

		public void AddPrerequisite(string prerequisite)
		{
			if (string.IsNullOrWhiteSpace(prerequisite))
				throw new ArgumentException("Prerequisite resolves to null or whitespace");

			if (prerequisite.StartsWith(":") && prerequisite.LastIndexOf(":", StringComparison.Ordinal) != 0)
				throw new ArgumentException("Prerequisite definition is invalid");
		
			if (prerequisites.Contains(prerequisite))
				throw new DuplicatePrerequisiteException(this, prerequisite);

			prerequisites.Add(prerequisite);
		}

		public abstract bool IsNeeded();
		public abstract DateTime Timestamp();
		
		protected abstract void DoExecute();
	}
}