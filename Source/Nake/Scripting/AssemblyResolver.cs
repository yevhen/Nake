using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Nake.Scripting
{
    public static class AssemblyResolver
    {
        static readonly Dictionary<string, MetadataFileReference> references = 
           new Dictionary<string, MetadataFileReference>();

        public static void Add(MetadataFileReference reference)
        {
            Debug.Assert(reference.Display != null);
            references.Add(Path.GetFileNameWithoutExtension(reference.Display), reference);
        }

        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name);
                if (assemblyName.Name == "Roslyn.Scripting")
                    return ScriptAssembly.Load();

                var reference = references.Find(assemblyName.Name);
                return reference != null 
                        ? Assembly.LoadFrom(reference.FullPath) 
                        : null;
            };
        }
    }
}
