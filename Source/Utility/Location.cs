using System;
using System.IO;

namespace Nake
{
    /// <summary>
    /// Convinience helper methods for working with file system paths
    /// </summary>
    public static class Location
    {
        /// <summary>
        /// Returns path to the current directory. By default it is a directory in which Nake was started.
        /// </summary>
        /// <remarks>Could be substituted dynamically</remarks>
        public static Func<string> CurrentDirectory = () => NakeStartupDirectory;

        /// <summary>
        /// Gets the script directory.
        /// </summary>
        /// <value> The directory in which entry script is residing. </value>
        public static string NakeScriptDirectory
        {
            get { return Env.Var["NakeScriptDirectory"]; }
        }

        /// <summary> 
        /// Gets the startup directory. 
        /// </summary>
        /// <value> The directory in which Nake was started. </value>
        public static string NakeStartupDirectory
        {
            get { return Env.Var["NakeStartupDirectory"]; }
        }

        internal static string GetRootedPath(string path, string basePath)
        {
            return Path.IsPathRooted(path)
                       ? path
                       : Path.Combine(basePath, path);
        }

        internal static FilePath GetFullPath(FilePath path) => 
            GetFullPath(path, FilePath.From(CurrentDirectory()));

        internal static FilePath GetFullPath(FilePath path, FilePath basePath) => 
            Path.IsPathRooted(path) ? path : basePath.Combine(path);
    }
}
