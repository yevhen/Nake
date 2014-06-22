using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nake.Scripting
{
    public static class AssemblyResolver
    {
        static readonly Dictionary<string, AssemblyReference> references =
           new Dictionary<string, AssemblyReference>();

        public static void Add(AssemblyReference reference)
        {
            references[reference.Name] = reference;
        }

        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name);
                if (assemblyName.Name == "Roslyn.Scripting")
                    return RoslynScriptingAssembly.Load();

                var reference = references.Find(assemblyName.Name);
                return reference != null 
                        ? Assembly.LoadFrom(reference.FullPath) 
                        : null;
            };
        }
    }
}
