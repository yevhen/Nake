using System;
using System.IO;

namespace Nake
{
	class DirectoryTask : Task
	{
		internal DirectoryTask(Project project, Scope scope, string name) 
			: base(project, scope, name)
		{}

		public override DateTime Timestamp()
		{
			return Directory.Exists(FullPath) ? DateTime.Now : DateTime.MinValue;
		}

		protected override void DoExecute()
		{
			Directory.CreateDirectory(FullPath);
		}

		public override bool IsNeeded()
		{
			return !Directory.Exists(FullPath);
		}
		
		string FullPath
		{
			get { return Location.GetFullPath(Key); }
		}
	}
}