using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

using TTask = System.Threading.Tasks.Task;

namespace Nake
{
	public class MSBuildSubmission : IEnumerable
	{
		public string ToolsVersion = "4.0";
		public LoggerVerbosity Verbosity = LoggerVerbosity.Normal;

		public bool BuildInParallel = true;
		public int MaxDegreeOfParallelism = Environment.ProcessorCount;

		readonly Dictionary<string, string> properties = new Dictionary<string, string>();
		readonly string[] projects;
		
		public MSBuildSubmission(IEnumerable<string> projects)
			: this(projects.ToArray())
		{}

		public MSBuildSubmission(params string[] projects)
		{
			this.projects = projects;
		}

		public void Add(string property, string value)
		{
			properties.Add(property, value);
		}

		public void Build(params string[] targets)
		{
			var logger = new ConsoleLogger(Verbosity)
			{
				Parameters = "ENABLEMPLOGGING;SHOWPROJECTFILE=TRUE;"
			};

			var parameters = new BuildParameters
			{
				Loggers = new[] {logger},
				MaxNodeCount = BuildInParallel ? MaxDegreeOfParallelism : 1,
			};

			var requests = projects.Select(project =>
				new BuildRequestData(project, properties, ToolsVersion, targets ?? new string[0], null)).ToArray();

			var manager = BuildManager.DefaultBuildManager;
			manager.BeginBuild(parameters);
			
			var results = requests.Select(request => 
				manager.PendBuildRequest(request).ExecuteAsync()).ToArray();

			manager.EndBuild();
			TTask.WhenAll(results).Wait();

			foreach (var result in results.Select(x => x.Result))
			{
				if (result.OverallResult != BuildResultCode.Success)
					throw result.Exception;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return properties.GetEnumerator();
		}
	}

	static class BuildSubmissionAwaitExtensions
	{
		public static Task<BuildResult> ExecuteAsync(this BuildSubmission submission)
		{
			var tcs = new TaskCompletionSource<BuildResult>();
			submission.ExecuteAsync(SetBuildComplete, tcs);
			return tcs.Task;
		}

		static void SetBuildComplete(BuildSubmission submission)
		{
			var tcs = (TaskCompletionSource<BuildResult>)submission.AsyncContext;
			tcs.SetResult(submission.BuildResult);
		}
	}
}
