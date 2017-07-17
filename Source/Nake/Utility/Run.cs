using System;
using System.Diagnostics;

namespace Nake.Utility
{
	/// <summary>
	/// Shortcuts for running external tools
	/// </summary>
	public static class Run
	{
		public class ExecException : Exception
		{
			public int ExitCode { get; set; }
			public string Stdout { get; set; }
			public string Stderr { get; set; }
		}
		
		public static void Exec(string command) { Exec(command, (e, stdout, stderr) => { }); }

		public static void Exec(string command, Action<Exception, string, string> action)
		{
			var commandIndexOf = command.IndexOf(" ");

			var process = Process.Start(new ProcessStartInfo
			{
				FileName = command.Substring(0, commandIndexOf),
				Arguments = command.Substring(commandIndexOf),
				RedirectStandardInput = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				UseShellExecute = false,
				StandardErrorEncoding = null,
				StandardOutputEncoding = null,
				WorkingDirectory = Location.CurrentDirectory()
			});

			process.Exited += (sender, e) =>
			{
				var ec = process.ExitCode;
				var stdout = process.StandardOutput.ReadToEnd();
				var stderr = process.StandardError.ReadToEnd();

				action(ec != 0
					? new ExecException
					{
						ExitCode = ec,
						Stderr = stderr,
						Stdout = stdout
					}
					: null, stdout, stderr);
			};

			if (!process.Start())
				action(new Exception("Unable to start command."), null, null);
		}
	}
}