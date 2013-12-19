using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Threading.Tasks;

using NuGet;

namespace Nake
{
    public class Program
    {
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            CatchUnhandledExceptions();

            RegisterRoslynResolver();
            DownloadRoslynPackage();

            StartApplication(args);
        }

        static void CatchUnhandledExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var e = (Exception) args.ExceptionObject;

                Log.Error(e);
                Exit.Fail(e);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                var e = args.Exception;
                args.SetObserved();

                foreach (var inner in e.Flatten().InnerExceptions)
                    Log.Error(inner);

                Exit.Fail(e);
            };
        }

        static void RegisterRoslynResolver()
        {
            RoslynAssemblyResolver.Register();
        }

        static void DownloadRoslynPackage()
        {
            const string roslynCompilerAssembly = "Roslyn.Compilers.CSharp";
            const string roslynVersion = "1.2.20906.2";
            
            if (GacUtil.IsAssemblyInGAC(roslynCompilerAssembly))
                return;

            const string roslynLocalPath = @"Roslyn\Roslyn.Compilers.Common.1.2.20906.2\lib\net45\Roslyn.Compilers.dll";
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, roslynLocalPath)))
                return;

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
            RoslynAssemblyResolver.Resolve = true;

            try
            {
                var options = Options.Parse(args);
                new Application(options).Start();
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
        }
    }
}
