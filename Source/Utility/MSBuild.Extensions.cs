using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nake
{
    /// <summary>
    /// Set of extension methods to make interoperating with MSBuild easier
    /// </summary>
    public static class MsBuildExtensions
    {
        /// <summary>
        /// Converts sequence of <see cref="ITaskItem"/> to string array
        /// </summary>
        /// <param name="sequence">Sequence of MSBuild task items</param>
        /// <returns>Array of strings</returns>
        public static string[] AsStrings(this IEnumerable<ITaskItem> sequence) => sequence.Select(x => x.ItemSpec).ToArray();

        /// <summary>
        /// Converts sequence of strings to array of <see cref="ITaskItem"/>
        /// </summary>
        /// <param name="sequence">Sequence of strings</param>
        /// <returns>Array of MSBuild task items</returns>
        public static ITaskItem[] AsTaskItems(this IEnumerable<string> sequence) => sequence.Select(AsTaskItem).ToArray();

        /// <summary>
        /// Converts string to <see cref="ITaskItem"/>
        /// </summary>
        /// <param name="s">string</param>
        /// <returns>Task item</returns>
        public static ITaskItem AsTaskItem(this string s) => new TaskItem(s);

        /// <summary>
        /// Executes MSBuild task.
        /// </summary>
        /// <typeparam name="TTask">The type of the task.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="quiet">if set to <c>true</c> completely disable any std out logging</param>
        /// <returns><c>true</c>, if successful</returns>
        public static bool Exec<TTask>(TTask task, bool quiet = false) where TTask : Task
        {
            task.BuildEngine = new MSBuildEngineStub(quiet);
            return task.Execute();
        }
    }
}
