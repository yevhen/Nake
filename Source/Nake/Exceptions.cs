using System;

namespace Nake
{
	public class NakeException : Exception
	{
		public NakeException(string message)
			: base(message)
		{}

		public NakeException(string message, params object[] args)
			: base(string.Format(message, args))
		{}
	}

	public class DuplicateDescriptionException : NakeException
	{
		public DuplicateDescriptionException()
			: base("Desc() is supposed to be followed by Task() definition")
		{}
	}	

	public class DuplicateTaskException : NakeException
	{
		public DuplicateTaskException(string target)
			: base("Task '{0}' already exists", target)
		{}
	}	
	
	public class DuplicatePrerequisiteException : NakeException
	{
		internal DuplicatePrerequisiteException(Task task, string prerequisite)
			: base("Cannot add duplicate prerequisite '{0}' to task '{1}'", prerequisite, task.DisplayName)
		{}
	}

	public class CyclicDependencyException : NakeException
	{
		internal CyclicDependencyException(Task from, Task to, string via)
			: base("Cyclic dependency detected from '{0}' to '{1}' via {2}", from.DisplayName, to.DisplayName, via)
		{}		
	}

	public class TaskNotFoundException : NakeException
	{
		internal TaskNotFoundException(string target)
			: base("Task '{0}' cannot be found", target)
		{}		
	}

	public class TaskPrerequisiteNotFoundException : NakeException
	{
		internal TaskPrerequisiteNotFoundException(Task task, string prerequisite)
			: base("Prerequisite '{0}' specified for task '{1}' cannot be found", prerequisite, task.DisplayName)
		{}		
	}
}
