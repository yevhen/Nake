using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Nake
{
    static class MSBuildTool
    {
        public static void Build(
            IEnumerable<string> projects,
            Dictionary<string, string> properties,
            string[] targets,
            bool buildInParallel,
            int maxDegreeOfParallelism,
            string toolsVersion,
            LoggerVerbosity verbosity)
        {
            var logger = new ConsoleLogger(verbosity)
            {
                Parameters = "ENABLEMPLOGGING;SHOWPROJECTFILE=TRUE;"
            };

            var parameters = new BuildParameters
            {
                Loggers = new[] {logger},
                MaxNodeCount = buildInParallel ? maxDegreeOfParallelism : 1,
            };

            var requests = projects
                .Select(project => CreateBuild(properties, targets, toolsVersion, project))
                .ToArray();

            var manager = BuildManager.DefaultBuildManager;
            manager.BeginBuild(parameters);
            
            var results = requests.Select(request => 
                manager.PendBuildRequest(request).ExecuteAsync()).ToArray();

            manager.EndBuild();
            Task.WhenAll(results).Wait();

            foreach (var result in results.Select(x => x.Result))
            {
                if (result.OverallResult != BuildResultCode.Success)
                    throw result.Exception;
            }
        }

        static BuildRequestData CreateBuild(IDictionary<string, string> properties, string[] targets, string toolsVersion, string project)
        {
            return new BuildRequestData(project, properties, toolsVersion, targets.Length != 0 ? targets : new []{"Build"}, null);
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
