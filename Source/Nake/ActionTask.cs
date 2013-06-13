using System;

namespace Nake
{
	public class ActionTask : Task
	{
		readonly Action<Task> action;

		internal ActionTask(Project project, Scope scope, string name, Action<Task> action)
			: base(project, scope, name)
		{
			this.action = action;
		}

		public override string Key
		{
			get { return ScopedTaskKey(); }
		}

		public override string DisplayName
		{
			get { return ScopedTaskDisplayName(); }
		}
		
		protected override void DoExecute()
		{
			action(this);
		}

		public override bool IsNeeded()
		{
			return true;
		}

		public override DateTime Timestamp()
		{
			return DateTime.Now;
		}
	}
}
