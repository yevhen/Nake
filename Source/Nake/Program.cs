using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

using NuGet;

namespace Nake
{
    public class Program
    {
        public static ManualResetEvent Downloading = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
           RegisterResolver();

           DownloadRoslyn();

           StartApplication(args);
        }

        static void RegisterResolver()
        {
            AssemblyResolver.Register();
        }

        static void DownloadRoslyn()
        {
            const string roslynCompilerAssembly = "Roslyn.Compilers.CSharp";
            const string roslynVersion = "1.2.20906.2";

            try
            {
                Assembly.Load(new AssemblyName(roslynCompilerAssembly));
                AssemblyResolver.Unregister();

                return;
            }
            catch (FileNotFoundException)
            {}

            Console.WriteLine("Roslyn CTP was not found");
            Console.WriteLine("Installing Roslyn CTP ...");

            var officialRepository = new AggregateRepository(
                PackageRepositoryFactory.Default, new[] { "https://nuget.org/api/v2/" }, true);

            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(currentDir != null, "currentDir != null");

            var packageManager = new PackageManager(officialRepository, Path.Combine(currentDir, "Roslyn"));
            packageManager.InstallPackage(roslynCompilerAssembly, new SemanticVersion(roslynVersion), false, true);

            Console.WriteLine("Roslyn CTP was installed successfully");
        }

        static void StartApplication(string[] args)
        {
            AssemblyResolver.Resolve = true;

            try
            {
                var application = new Application(Options.Parse(args));
                application.Start();
            }
            catch (OptionParseException e)
            {
                Log.Error(e.Message);
                Options.PrintUsage();
                Exit.Fail(e);
            }
            catch (TaskInvocationException e)
            {
                Log.Error(e.SourceException);
                Exit.Fail(e);
            }
            catch (Exception e)
            {
                Log.Error(e);
                Exit.Fail(e);
            }
        }
    }
}
