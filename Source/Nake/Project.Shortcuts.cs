using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Tasks;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Nake
{
	partial class Project
	{
		public static readonly Env Env = new Env();

		public void Log(string message, params object[] args)
		{
			if (message == null)
				throw new ArgumentException("Log() doesn't accept null message");

			Out.LogFormat(message, args);
		}

		public void Abort(string message, params object[] args)
		{
			if (message == null)
				throw new ArgumentException("Abort() doesn't accept null message");

			Exit.Fail(message, args);
		}

		public void Exec(string command)
		{
			Run(new Exec
			{
				Command = command, 
				EchoOff = true, 
				WorkingDirectory = Location.CurrentDirectory(),
				LogStandardErrorAsError = true,				
				EnvironmentVariables = Env.All().ToArray(),
			});			
		}

		public void Copy(FileList sourceFiles, string destinationFolder, bool overwriteReadOnlyFiles = false)
		{
			Run(new Copy
			{
				SourceFiles = sourceFiles.AsTaskItems(),
				OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
				DestinationFolder = destinationFolder.AsTaskItem(),
			});
		}

		public void Move(FileList sourceFiles, string destinationFolder, bool overwriteReadOnlyFiles = false)
		{
			Run(new Move
			{
				SourceFiles = sourceFiles.AsTaskItems(),
				OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
				DestinationFolder = destinationFolder.AsTaskItem(),
			});
		}

		public void Delete(params string[] files)
		{
			Delete((FileList)files);
		}

		public void Delete(IEnumerable<string> files)
		{
			Delete((FileList)files);
		}

		public void Delete(FileList files)
		{
			Run(new Delete
			{
				Files = files.AsTaskItems()
			});
		}

		public void MakeDir(params string[] directories)
		{
			MakeDir((FileList)directories);
		}

		public void MakeDir(IEnumerable<string> directories)
		{
			MakeDir((FileList)directories);
		}

		public void MakeDir(FileList directories)
		{
			Run(new MakeDir
			{
				Directories = directories.AsTaskItems()
			});
		}

		public void RemoveDir(params string[] directories)
		{
			RemoveDir((FileList)directories);
		}

		public void RemoveDir(IEnumerable<string> directories)
		{
			RemoveDir((FileList)directories);		
		}

		public void RemoveDir(FileList directories)
		{
			Run(new RemoveDir
			{
				Directories = directories.AsTaskItems()
			});			
		}

		public void RemoveDirContents(params string[] directories)
		{
			RemoveDirContents((FileList)directories);
		}

		public void RemoveDirContents(IEnumerable<string> directories)
		{
			RemoveDirContents((FileList)directories);
		}

		public void RemoveDirContents(FileList directories)
		{
			foreach (var directory in directories)
			{
				Exec(String.Format(@"DEL /S /Q /F {0}", Path.Combine(Location.GetFullPath(directory), "*.*")));
				Exec(String.Format(@"FOR /D %%p IN (""{0}"") DO RMDIR /S /Q ""%%p""", Path.Combine(Location.GetFullPath(directory), "*.*")));
			}
		}

		public static MSBuildTask Run(MSBuildTask task)
		{
			task.BuildEngine = new MSBuildEngineStub();

			if (!task.Execute() || task.Log.HasLoggedErrors)
				Exit.Fail("{0} failed", task.GetType());

			return task;
		}

		public MSBuildProjects Projects(params string[] projects)
		{
			return Projects((FileList)projects);
		}

		public MSBuildProjects Projects(IEnumerable<string> projects)
		{
			return Projects((FileList)projects);
		}

		public MSBuildProjects Projects(FileList projects)
		{
			return new MSBuildProjects(projects);
		}
	}
}
