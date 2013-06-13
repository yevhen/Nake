using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Nake
{
	public class MSBuildProject : IEnumerable
	{
		public string ToolsVersion = "4.0";
		public LoggerVerbosity Verbosity = LoggerVerbosity.Normal;

		public bool BuildInParallel = true;
		public int MaxDegreeOfParallelism = Environment.ProcessorCount;

		readonly string project;
		readonly Dictionary<string, string> properties = new Dictionary<string, string>();

		public MSBuildProject(string project)
		{
			this.project = project;
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
				MaxNodeCount = BuildInParallel ? MaxDegreeOfParallelism : 1
			};

			var request = new BuildRequestData(project, properties, ToolsVersion, targets ?? new string[0], null);
			var result = BuildManager.DefaultBuildManager.Build(parameters, request);

			if (result.OverallResult != BuildResultCode.Success)
				throw result.Exception;
		}

		public IEnumerator GetEnumerator()
		{
			return properties.GetEnumerator();
		}
	}
}
