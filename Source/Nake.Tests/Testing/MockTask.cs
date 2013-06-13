using System;

namespace Nake.Tests.Testing
{
	class MockTask : ActionTask
	{
		readonly Action action;

		public bool Needed = true;
		public DateTime MockTimestamp = DateTime.Now;

		internal MockTask(Project project, Scope scope, Action action)
			: base(project, scope, Guid.NewGuid().ToString(), t => action())
		{
			this.action = action;
		}

		protected override void DoExecute()
		{
			action();
		}

		public override bool IsNeeded()
		{
			return Needed;
		}

		public override DateTime Timestamp()
		{
			return MockTimestamp;
		}
	}
}