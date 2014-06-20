using System;
using System.IO;
using System.Linq;

using Microsoft.Build.Tasks;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Nake
{
    /// <summary>
    /// File-system convinience methods
    /// </summary>
    public static class FS
    {
        /// <summary>
        /// Copies the specified files into specified destination files.
        /// </summary>
        /// <param name="sourceFiles">The source files.</param>
        /// <param name="destinationFiles">The destination files.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        /// <param name="skipUnchangedFiles">if set to <c>true</c> will skip unchanged files. Default is <c>true</c></param>
        public static void Copy
        (
            string[] sourceFiles, 
            string[] destinationFiles, 
            bool overwriteReadOnlyFiles = false, 
            bool skipUnchangedFiles = true)
        {
            Execute(new Copy
            {
                SourceFiles = sourceFiles.AsTaskItems(),
                DestinationFiles = destinationFiles.AsTaskItems(),
                OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
                SkipUnchangedFiles = skipUnchangedFiles
            });
        }

        /// <summary>
        /// Copies the specified files into a specifed destination folder.
        /// </summary>
        /// <param name="sourceFiles">The source files.</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        /// <param name="skipUnchangedFiles">if set to <c>true</c> will skip unchanged files. Default is <c>true</c></param>
        public static void Copy
        (
            string[] sourceFiles, 
            string destinationFolder, 
            bool overwriteReadOnlyFiles = false, 
            bool skipUnchangedFiles = true)
        {
            Execute(new Copy
            {
                SourceFiles = sourceFiles.AsTaskItems(),
                DestinationFolder = destinationFolder.AsTaskItem(),
                OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
                SkipUnchangedFiles = skipUnchangedFiles
            });
        }

        /// <summary>
        /// Copies the files, specified as file selection patterns, into a specifed destination folder.
        /// </summary>
        /// <param name="sourceFiles">The source file selection patterns</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        /// <param name="skipUnchangedFiles">if set to <c>true</c> will skip unchanged files. Default is <c>true</c></param>
        public static void Copy
        (
            string sourceFiles, 
            string destinationFolder, 
            bool overwriteReadOnlyFiles = false, 
            bool skipUnchangedFiles = true)
        {
            Copy(new FileSet(sourceFiles).ToArray(), destinationFolder, overwriteReadOnlyFiles, skipUnchangedFiles);
        }

        /// <summary>
        /// Moves the specified files into specified destination files.
        /// </summary>
        /// <param name="sourceFiles">The source files.</param>
        /// <param name="destinationFiles">The destination files.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        public static void Move(
            string[] sourceFiles, 
            string[] destinationFiles, 
            bool overwriteReadOnlyFiles = false)
        {
            Execute(new Move
            {
                SourceFiles = sourceFiles.AsTaskItems(),
                OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
                DestinationFiles = destinationFiles.AsTaskItems(),                
            });
        }

        /// <summary>
        /// Moves the specified files into a specifed destination folder..
        /// </summary>
        /// <param name="sourceFiles">The source files.</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        public static void Move(
            string[] sourceFiles, 
            string destinationFolder, 
            bool overwriteReadOnlyFiles = false)
        {
            Execute(new Move
            {
                SourceFiles = sourceFiles.AsTaskItems(),
                OverwriteReadOnlyFiles = overwriteReadOnlyFiles,
                DestinationFolder = destinationFolder.AsTaskItem(),                
            });
        }

        /// <summary>
        /// Moves the files, specified as file selection patterns, into a specifed destination folder.
        /// </summary>
        /// <param name="sourceFiles">The source file selection patterns</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="overwriteReadOnlyFiles">if set to <c>true</c> will overwrite read only files. Default is <c>false</c></param>
        public static void Move(
            string sourceFiles, 
            string destinationFolder, 
            bool overwriteReadOnlyFiles = false)
        {
            Move(new FileSet(sourceFiles).ToArray(), destinationFolder, overwriteReadOnlyFiles);
        }

        /// <summary>
        /// Deletes the specified files.
        /// </summary>
        /// <param name="files">The file selection patterns.</param>
        /// <returns>Array of actually deleted files</returns>
        public static string[] Delete(params string[] files)
        {
            return Execute(new Delete
            {
                Files = new FileSet().Add(files).AsTaskItems(),
                TreatErrorsAsWarnings = false
            })
            .DeletedFiles.AsStrings();
        }

        /// <summary>
        /// Makes specified directories.
        /// </summary>
        /// <param name="directories">The directories.</param>
        public static void MakeDir(params string[] directories)
        {
            Execute(new MakeDir
            {
                Directories = directories.AsTaskItems()
            });
        }

        /// <summary>
        /// Removes specified directories.
        /// </summary>
        /// <param name="directories">The directories.</param>
        /// <returns>Array of actually removed directories</returns>
        public static string[] RemoveDir(params string[] directories)
        {
            return Execute(new RemoveDir
            {                
                Directories = new FileSet().Add(directories).AsTaskItems()			
            })
            .RemovedDirectories.AsStrings();
        }

        /// <summary>
        /// Checks that specified output file is up to date in respect to specified set of input files.
        /// </summary>
        /// <param name="outputFile">The output file</param>
        /// <param name="inputFiles">The input files.</param>
        /// <returns><c>true</c> if output file is up to date or doesn't exists, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentException">Specified input files do not exist</exception>
        public static bool UpToDate(string outputFile, params string[] inputFiles)
        {
            if (!File.Exists(Location.GetFullPath(outputFile)))
                return false;

            var outdated = false;

            foreach (var inputFile in new FileSet().Add(inputFiles))
            {
                if (!File.Exists(inputFile))
                    throw new ArgumentException("Specified input file does not exists: " + inputFile);

                if (File.GetLastWriteTime(inputFile) <= File.GetLastWriteTime(Location.GetFullPath(outputFile)))
                    continue;

                outdated = true;
                break;
            }

            return !outdated;
        }

        static TTask Execute<TTask>(TTask task) where TTask : MSBuildTask
        {
            return Run.MSBuild(task);
        }
    }
}
