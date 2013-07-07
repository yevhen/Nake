using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using NuGet;

namespace Nake
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			DownloadDependencies();

			try
			{
				var application = new Application(Options.Parse(args));
				application.Start();
			}
			catch (OptionParseException e)
			{
				Out.Fail(e.Message);
				Options.PrintUsage();
				Exit.Fail(e);
			}
			catch (Exception e)
			{
				Out.Fail(e);	
				Exit.Fail(e);
			}
		}

		[Conditional("RELEASE")]
		static void DownloadDependencies()
		{
			var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			try
			{
				Assembly.Load(new AssemblyName("Roslyn.Compilers.CSharp, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
				return;
			}
			catch (FileNotFoundException)
			{}

			if (File.Exists(Path.Combine(currentDir, @"Dependencies\Roslyn.Compilers.CSharp.1.2.20906.2\lib\net45\Roslyn.Compilers.CSharp.dll")))
				return;

			Console.WriteLine("Roslyn CTP was not found");
			Console.WriteLine("Installing Roslyn CTP ...");

			var officialRepository = new AggregateRepository(
				PackageRepositoryFactory.Default, new[] { "https://nuget.org/api/v2/" }, true);

			var packageManager = new PackageManager(officialRepository, Path.Combine(currentDir, "Dependencies"));
			packageManager.InstallPackage("Roslyn.Compilers.CSharp", new SemanticVersion("1.2.20906.2"), false, true);

			Console.WriteLine("Roslyn CTP was installed successfully");
		}
	}
}
