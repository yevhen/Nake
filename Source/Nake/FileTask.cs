using System;
using System.IO;
using System.Linq;

namespace Nake
{
	public class FileTask : Task
	{
		readonly Action<FileTask> action;

		internal FileTask(Project project, Scope scope, string name, Action<FileTask> action)
			: base(project, scope, name)
		{
			this.action = action;
		}

		protected override void DoExecute()
		{
			action(this);
		}
		
		public override DateTime Timestamp()
		{
			return File.Exists(FullPath)
					   ? File.GetLastWriteTime(FullPath) 
					   : DateTime.MinValue;
		}

		public override bool IsNeeded()
		{
			return !File.Exists(FullPath) || Outdated(Timestamp());
		}

		bool Outdated(DateTime timestamp)
		{
			return PrerequisiteTasks().Any(dependency => dependency.Timestamp() > timestamp);
		}

		public string FullPath
		{
			get { return Location.GetFullPath(Key); }
		}
	}
}