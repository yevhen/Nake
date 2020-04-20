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

        public static void Add(AssemblyReference reference) => 
            references[reference.Name] = reference;

        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var reference = references.Find(new AssemblyName(args.Name!).Name);
                
                return reference != null 
                        ? Assembly.LoadFrom(reference.FullPath) 
                        : null;
            };
        }
    }
}
