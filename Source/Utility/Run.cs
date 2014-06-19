using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nake
{
    public static class Run
    {
        public static int Cmd(string command)
        {
            var task = new Exec
            {
                Command = command,
                EchoOff = true,
                WorkingDirectory = Location.CurrentDirectory(),
                LogStandardErrorAsError = true,
                EnvironmentVariables = Env.Var.All(),
                BuildEngine = new MSBuildEngineStub(),
            };

            if (!task.Execute() || (((Task) task).Log.HasLoggedErrors && !true))
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));
            
            return task.ExitCode;
        }

        public static void MSBuild(
            string solutionOrSingleProject,
            string properties = null,
            string targets = null,
            bool buildInParallel = true,
            int? maxDegreeOfParallelism = null,
            string toolsVersion = "4.0",
            MSBuildVerbosity verbosity = null)
        {
            MSBuild(new FileSet {solutionOrSingleProject}, 
                properties, targets, 
                buildInParallel, maxDegreeOfParallelism, 
                toolsVersion, verbosity);
        }

        public static void MSBuild(
            FileSet projects, 
            string properties = null, 
            string targets = null, 
            bool buildInParallel = true,
            int? maxDegreeOfParallelism = null,
            string toolsVersion = "4.0", 
            MSBuildVerbosity verbosity = null)
        {
            MSBuildTool.Build(projects,
                SplitProperties(properties ?? ""), SplitTargets(targets ?? ""),
                buildInParallel, maxDegreeOfParallelism ?? Environment.ProcessorCount,
                toolsVersion, verbosity != null ? verbosity.Level : LoggerVerbosity.Normal);
        }

        static Dictionary<string, string> SplitProperties(string properties)
        {
            return properties
                    .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new { key = x.Split('=')[0], value = x.Split('=')[1] })
                    .ToDictionary(x => x.key, x => x.value);
        }

        static string[] SplitTargets(string targets)
        {
            return targets.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static TTask MSBuild<TTask>(TTask task, bool ignoreLogErrors = true) where TTask : Task
        {
            task.BuildEngine = new MSBuildEngineStub();

            if (!task.Execute() || (task.Log.HasLoggedErrors && !ignoreLogErrors))
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));

            return task;
        }
    }

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
}
