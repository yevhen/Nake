using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Nake
{
    public class MSBuildVerbosity
    {
        internal readonly LoggerVerbosity Level;

        public static readonly MSBuildVerbosity Detailed = new MSBuildVerbosity(LoggerVerbosity.Detailed);
        public static readonly MSBuildVerbosity Diagnostic = new MSBuildVerbosity(LoggerVerbosity.Diagnostic);
        public static readonly MSBuildVerbosity Minimal = new MSBuildVerbosity(LoggerVerbosity.Minimal);
        public static readonly MSBuildVerbosity Normal = new MSBuildVerbosity(LoggerVerbosity.Normal);
        public static readonly MSBuildVerbosity Quiet = new MSBuildVerbosity(LoggerVerbosity.Quiet);

        MSBuildVerbosity(LoggerVerbosity level)
        {
            Level = level;
        }
    }

    public class MSBuildProjects
    {
        string toolsVersion = "4.0";
        LoggerVerbosity loggerVerbosity = LoggerVerbosity.Normal;

        bool buildInParallel;
        int maxDegreeOfParallelism = Environment.ProcessorCount;

        readonly Dictionary<string, string> buildProperties = new Dictionary<string, string>();
        readonly HashSet<string> buildProjects = new HashSet<string>();
        readonly HashSet<string> buildTargets = new HashSet<string>();

        internal MSBuildProjects(IEnumerable<string> projects)
        {
            buildProjects.UnionWith(projects.ToArray());
        }

        public MSBuildProjects ToolsVersion(string version)
        {
            toolsVersion = version;
            return this;
        }

        public MSBuildProjects Verbosity(MSBuildVerbosity verbosity)
        {
            loggerVerbosity = verbosity.Level;
            return this;
        }

        public MSBuildProjects MaxDegreeOfParallelism(int degree)
        {
            maxDegreeOfParallelism = degree;
            return this;
        }

        public MSBuildProjects Properties(string properties)
        {
            return Properties(
                properties
                    .Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new {key = x.Split('=')[0], value = x.Split('=')[1]})
                    .ToDictionary(x => x.key, x => x.value)
            );
        }

        public MSBuildProjects Properties(IDictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                Property(property.Key, property.Value);
            }

            return this;
        }

        public MSBuildProjects Property(string property, string value)
        {
            buildProperties[property] = value;
            return this;
        }

        public MSBuildProjects Targets(params string[] targets)
        {
            buildTargets.UnionWith(targets);
            return this;
        }

        public void BuildInParallel()
        {
            buildInParallel = true;

            Build();
        }

        public void Build()
        {
            var logger = new ConsoleLogger(loggerVerbosity)
            {
                Parameters = "ENABLEMPLOGGING;SHOWPROJECTFILE=TRUE;"
            };

            var parameters = new BuildParameters
            {
                Loggers = new[] {logger},
                MaxNodeCount = buildInParallel ? maxDegreeOfParallelism : 1,
            };

            var requests = buildProjects.Select(project =>
                new BuildRequestData(project, buildProperties, toolsVersion, buildTargets.ToArray(), null)).ToArray();

            var manager = BuildManager.DefaultBuildManager;
            manager.BeginBuild(parameters);
            
            var results = requests.Select(request => 
                manager.PendBuildRequest(request).ExecuteAsync()).ToArray();

            manager.EndBuild();
            System.Threading.Tasks.Task.WhenAll(results).Wait();

            foreach (var result in results.Select(x => x.Result))
            {
                if (result.OverallResult != BuildResultCode.Success)
                    throw result.Exception;
            }
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
