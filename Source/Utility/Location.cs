using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Nake;

/// <summary>
/// Convenience helper methods for working with script related file system locations
/// </summary>
public static class Location
{
    /// <summary> 
    /// Gets the calling script path
    /// </summary>
    /// <returns> The file path</returns>
    public static string NakeScriptPath([CallerFilePath] string path = null) => path!;

    /// <summary> 
    /// Gets the calling script directory
    /// </summary>
    /// <returns> The directory path</returns>
    public static string NakeScriptDirectory([CallerFilePath] string path = null) => 
        Path.GetDirectoryName(path) ?? "";
        
    /// <summary> 
    /// Gets the working directory passed to Nake runner.
    /// If not specified from cli will return <see cref="NakeStartupDirectory"/>
    /// </summary>
    /// <returns> The directory path</returns>
    public static string NakeWorkingDirectory { get; } = Env.Var["NakeWorkingDirectory"] ?? Environment.CurrentDirectory;

    /// <summary> 
    /// Gets the Nake startup directory, which is whatever <see cref="Environment.CurrentDirectory"/>
    /// was pointing to at the time the Nake was started. 
    /// </summary>
    /// <returns> The directory path</returns>
    public static string NakeStartupDirectory { get; } = Env.Var["NakeStartupDirectory"] ?? Environment.CurrentDirectory;

    /// <summary>
    /// Get or set the path to the current working directory.
    /// By default, if not overriden from cli, it is a directory in which Nake was started.
    /// </summary>
    public static string CurrentDirectory { get; set; } = NakeWorkingDirectory ?? Environment.CurrentDirectory;

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