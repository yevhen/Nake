using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Nake.Tests
{
	[TestFixture]
	public class IntegrationFixture
	{
		[Test]
		public void No_exceptions_for_valid_usage()
		{
			Program.Main(Args("--trace Property='Set in Nake' Switch=True Number=100 Target"));
			Out.TraceEnabled = false;
		}		
		
		static string[] Args(string commandLine)
		{
			commandLine = commandLine.Replace("'", "\"");
			return Parse(string.Format("-f \"{0}\" {1}", Path.Combine(BaseDirectory(), "Nake.csx"), commandLine));
		}

		static string BaseDirectory()
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Testing\Integration");
		}

		public static string[] Parse(string commandLine)
		{
			int argc;
			var argv = CommandLineToArgvW(commandLine, out argc);
			if (argv == IntPtr.Zero)
				throw new System.ComponentModel.Win32Exception();

			try
			{
				var args = new string[argc];
				for (var i = 0; i < args.Length; i++)
				{
					var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
					args[i] = Marshal.PtrToStringUni(p);
				}

				return args;
			}
			finally
			{
				Marshal.FreeHGlobal(argv);
			}
		}

		[DllImport("shell32.dll", SetLastError = true)]
		static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);
	}
}