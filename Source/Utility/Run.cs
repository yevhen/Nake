using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nake
{
    /// <summary>
    /// Shortcuts for running external tools
    /// </summary>
    public static class Run
    {
        /// <summary>
        /// Executes specified command within a standard OS command-line interpreter.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="environmentVariables">The environment variables pairs to pass. Default is all vars defined within a process</param>
        /// <param name="workingDirectory">The working directory. Default is current directory</param>
        /// <param name="echoOff">if set to <c>true</c>disables echoing command output to std out</param>
        /// <param name="ignoreStdOutErrors">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="ignoreExitCode">if set to <c>true</c> ignores exit code</param>
        /// <param name="disableStdOutLogging">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns> Exit code </returns>
        /// <exception cref="System.ApplicationException">If command fails</exception>
        public static int Cmd(
            string command, 
            string[] environmentVariables = null, 
            string workingDirectory = null, 
            bool echoOff = true,
            bool ignoreStdOutErrors = false, 
            bool ignoreExitCode = false,
            bool disableStdOutLogging = false)
        {
            var task = new Exec
            {   
                Command = command,
                EchoOff = echoOff,
                WorkingDirectory = workingDirectory ?? Location.CurrentDirectory(),
                LogStandardErrorAsError = !ignoreStdOutErrors,
                IgnoreExitCode = ignoreExitCode,
                EnvironmentVariables = environmentVariables ?? Env.Var.All(),
                BuildEngine = new MSBuildEngineStub(disableStdOutLogging),
            };
            
            if (!task.Execute() || task.Log.HasLoggedErrors)
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));
            
            return task.ExitCode;
        }

        /// <summary>
        /// Runs MSBuild tool
        /// </summary>
        /// <param name="solutionOrSingleProject">The solution or single project.</param>
        /// <param name="properties">The properties to pass.</param>
        /// <param name="targets">The targets to execute.</param>
        /// <param name="buildInParallel">if set to <c>true</c> will build projects in parallel.</param>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
        /// <param name="toolsVersion">The tools version.</param>
        /// <param name="verbosity">The log verbosity.</param>
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

        /// <summary>
        /// Runs MSBuild tool
        /// </summary>
        /// <param name="projects">The set of projects.</param>
        /// <param name="properties">The properties to pass.</param>
        /// <param name="targets">The targets to execute.</param>
        /// <param name="buildInParallel">if set to <c>true</c> will build projects in parallel.</param>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
        /// <param name="toolsVersion">The tools version.</param>
        /// <param name="verbosity">The log verbosity.</param>
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

        /// <summary>
        /// Executes MSBuild task.
        /// </summary>
        /// <typeparam name="TTask">The type of the task.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="ignoreStdOutErrors">if set to <c>true</c> ignores errors logged to std out</param>
        /// <param name="disableStdOutLogging">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns>The executed task. Allows to get value of any OUT property</returns>
        /// <exception cref="System.ApplicationException">If tasks fails</exception>
        public static TTask MSBuild<TTask>(
            TTask task, 
            bool ignoreStdOutErrors = true,
            bool disableStdOutLogging = false) where TTask : Task
        {
            task.BuildEngine = new MSBuildEngineStub(disableStdOutLogging);

            if (!task.Execute() || (task.Log.HasLoggedErrors && !ignoreStdOutErrors))
                throw new ApplicationException(string.Format("{0} failed", task.GetType()));

            return task;
        }
    }

    /// <summary>
    /// The MSBuild logger verbosity
    /// </summary>
    public class MSBuildVerbosity
    {
        internal readonly LoggerVerbosity Level;

        /// <summary>
        /// Detailed verbosity, which displays errors, warnings, messages with high or normal importance, all status events, and a build summary.
        /// </summary>
        public static readonly MSBuildVerbosity Detailed = new MSBuildVerbosity(LoggerVerbosity.Detailed);

        /// <summary>
        /// Diagnostic verbosity, which displays all errors, warnings, messages, status events, and a build summary.
        /// </summary>
        public static readonly MSBuildVerbosity Diagnostic = new MSBuildVerbosity(LoggerVerbosity.Diagnostic);

        /// <summary>
        /// Minimal verbosity, which displays errors, warnings, messages with high importance, and a build summary.
        /// </summary>
        public static readonly MSBuildVerbosity Minimal = new MSBuildVerbosity(LoggerVerbosity.Minimal);

        /// <summary>
        /// Normal verbosity, which displays errors, warnings, messages with high importance, some status events, and a build summary.
        /// </summary>
        public static readonly MSBuildVerbosity Normal = new MSBuildVerbosity(LoggerVerbosity.Normal);

        /// <summary>
        /// Quiet verbosity, which displays a build summary.
        /// </summary>
        public static readonly MSBuildVerbosity Quiet = new MSBuildVerbosity(LoggerVerbosity.Quiet);

        MSBuildVerbosity(LoggerVerbosity level)
        {
            Level = level;
        }
    }
}
