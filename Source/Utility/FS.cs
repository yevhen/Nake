using System;
using System.IO;
using System.Linq;

using Microsoft.Build.Tasks;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Nake
{
    public static class FS
    {
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

        public static string[] Delete(params string[] files)
        {
            return Execute(new Delete
            {
                Files = new FileSet(files).AsTaskItems(),
                TreatErrorsAsWarnings = false
            })
            .DeletedFiles.AsStrings();
        }

        public static void MakeDir(params string[] directories)
        {
            Execute(new MakeDir
            {
                Directories = directories.AsTaskItems()
            });
        }

        public static string[] RemoveDir(params string[] directories)
        {
            return Execute(new RemoveDir
            {                
                Directories = new FileSet(directories).AsTaskItems()			
            })
            .RemovedDirectories.AsStrings();
        }

        public static bool UpToDate(string outputFile, params string[] inputFiles)
        {
            if (!File.Exists(Location.GetFullPath(outputFile)))
                return false;

            var outdated = false;

            foreach (var inputFile in new FileSet(inputFiles))
            {
                if (!File.Exists(inputFile))
                    throw new ArgumentException("Specified input file does not exists: " + inputFile);

                if (File.GetLastWriteTime(inputFile) > File.GetLastWriteTime(Location.GetFullPath(outputFile)))
                {
                    outdated = true;
                    break;
                }
            }

            return !outdated;
        }

        static TTask Execute<TTask>(TTask task) where TTask : MSBuildTask
        {
            return MSBuild.Execute(task);
        }
    }
}
