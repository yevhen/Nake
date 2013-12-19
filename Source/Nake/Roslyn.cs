using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NuGet;

namespace Nake
{
    class Roslyn
    {
        static string currentDir;
        static List<RoslynAssembly> roslynAssemblies;

        public static void Bootstrap()
        {
            currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            roslynAssemblies = new List<RoslynAssembly>
            {
                new RoslynAssembly("Roslyn.Compilers", "Roslyn.Compilers.Common"),
                new RoslynAssembly("Roslyn.Compilers.CSharp", "Roslyn.Compilers.CSharp")
            };

            if (RoslynPackage.Installed())
                return;

            AssemblyResolver.Register();
            RoslynPackage.Download();
        }

        static class RoslynPackage
        {
            const string Name = "Roslyn.Compilers.CSharp";
            public const string Version = "1.2.20906.2";

            public static bool Installed()
            {
                return IsAssemblyInGac() || ExistsLocally();
            }

            static bool IsAssemblyInGac()
            {
                return GacUtil.IsAssemblyInGac(Name);
            }

            static bool ExistsLocally()
            {
                return File.Exists(Path.Combine(currentDir, @"Roslyn\" + Name + "." + Version));
            }

            public static void Download()
            {
                Console.WriteLine("Roslyn CTP was not found");
                Console.WriteLine("Installing Roslyn CTP ...");

                var officialRepository = new AggregateRepository(
                    PackageRepositoryFactory.Default, new[] { "https://nuget.org/api/v2/" }, true);

                var packageManager = new PackageManager(officialRepository, Path.Combine(currentDir, "Roslyn"));
                packageManager.InstallPackage(Name, new SemanticVersion(Version), false, true);

                Console.WriteLine("Roslyn CTP was installed successfully");
            }
        }

        class RoslynAssembly
        {
            public readonly string AssemblyName;
            readonly string assemblyPath;

            public RoslynAssembly(string assemblyName, string packageName)
            {
                AssemblyName = assemblyName;

                assemblyPath = Path.Combine(currentDir,
                    string.Format(@"Roslyn\{0}.{1}\lib\net45\{2}.dll", packageName, RoslynPackage.Version, assemblyName));
            }

            public Assembly Load()
            {
                return Assembly.LoadFrom(assemblyPath);
            }
        }

        static class AssemblyResolver
        {
            static readonly ConcurrentDictionary<string, Lazy<Assembly>> resolvedAssemblies =
                new ConcurrentDictionary<string, Lazy<Assembly>>();

            internal static void Register()
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            }

            static void Unregister()
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            }

            static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
            {
                return resolvedAssemblies.GetOrAdd(args.Name, LoadRoslynAssembly).Value;
            }

            static Lazy<Assembly> LoadRoslynAssembly(string name)
            {
                return new Lazy<Assembly>(() =>
                {
                    var assemblyName = new AssemblyName(name);

                    var roslynAssembly = roslynAssemblies
                        .Find(x => x.AssemblyName == assemblyName.Name);

                    Unregister();

                    return roslynAssembly.Load();
                });
            }
        }
    }
}
