using System;
using System.IO;

namespace Nake
{
    /// <summary>
    /// Convenience helper methods for working with script related file system locations
    /// </summary>
    public static class Location
    {
        /// <summary> 
        /// Gets the working directory passed to Nake runner.
        /// If not specified from cli will return <see cref="NakeStartupDirectory"/>
        /// </summary>
        /// <value> The directory path</value>
        public static string NakeWorkingDirectory { get; } = Env.Var["NakeWorkingDirectory"];

        /// <summary> 
        /// Gets the Nake startup directory, which is whatever <see cref="Environment.CurrentDirectory"/>
        /// was pointing to at the time the Nake was started. 
        /// </summary>
        /// <value> The directory path</value>
        public static string NakeStartupDirectory { get; } = Env.Var["NakeStartupDirectory"];

        /// <summary>
        /// Get or set the path to the current working directory.
        /// By default, if not overriden from cli, it is a directory in which Nake was started.
        /// </summary>
        public static string CurrentDirectory { get; set; } = NakeWorkingDirectory;

        internal static string GetRootedPath(string path, string basePath)
        {
            return Path.IsPathRooted(path)
                       ? path
                       : Path.Combine(basePath, path);
        }

        internal static FilePath GetFullPath(FilePath path) => 
            GetFullPath(path, FilePath.From(CurrentDirectory));

        internal static FilePath GetFullPath(FilePath path, FilePath basePath) => 
            Path.IsPathRooted(path) ? path : basePath.Combine(path);
    }
}
