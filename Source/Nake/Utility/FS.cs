using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nake.Utility
{
	/// <summary>
	/// File-system convenience methods
	/// </summary>
	public static class FS
	{
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
			Parallel.ForEach(sourceFiles, (file) =>
			{
				var fileName = Path.GetFileName(file);
				var destFile = Path.Combine(destinationFolder, fileName);

				if (skipUnchangedFiles && UpToDate(destFile, file)) return;
				File.Copy(file, destFile, overwriteReadOnlyFiles);
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
			Copy(new FileSet().Add(sourceFiles).ToArray(), destinationFolder, overwriteReadOnlyFiles, skipUnchangedFiles);
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
			Parallel.ForEach(sourceFiles, (file) =>
			{
				var fileName = Path.GetFileName(file);
				var destFile = Path.Combine(destinationFolder, fileName);

				if (overwriteReadOnlyFiles && File.Exists(destFile)) File.Delete(destFile);
				File.Move(file, destFile);
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
			Move(new FileSet().Add(sourceFiles).ToArray(), destinationFolder, overwriteReadOnlyFiles);
		}

		/// <summary>
		/// Deletes the specified files.
		/// </summary>
		/// <param name="files">The file selection patterns.</param>
		/// <returns>Array of actually deleted files</returns>
		public static string[] Delete(params string[] files)
		{
			Parallel.ForEach(files, (file) => { File.Delete(file); });
			return files;
		}

		/// <summary>
		/// Makes specified directories.
		/// </summary>
		/// <param name="directories">The directories.</param>
		public static void MakeDir(params string[] directories)
		{
			Parallel.ForEach(directories, (dir) => { Directory.CreateDirectory(dir); });
		}

		/// <summary>
		/// Removes specified directories.
		/// </summary>
		/// <param name="directories">The directories.</param>
		/// <returns>Array of actually removed directories</returns>
		public static string[] RemoveDir(params string[] directories)
		{
			Parallel.ForEach(directories, (dir) => { Directory.Delete(dir, true); });
			return directories;
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
	}
}