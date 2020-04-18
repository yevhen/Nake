using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Dotnet.Script.DependencyModel.Compilation;
using Dotnet.Script.DependencyModel.Context;
using Dotnet.Script.DependencyModel.Logging;
using Dotnet.Script.DependencyModel.NuGet;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Nake.Scripting
{
    class Script
    {
        static readonly List<MetadataReference> NakeReferences = new List<MetadataReference>
        {
            Reference(typeof(Engine)),
        };

        static readonly Dictionary<string, MetadataReference> DefaultReferences = new Dictionary<string, MetadataReference>
        {
            {"mscorlib",                        Reference(typeof(object))},          
            {"System.Runtime",                  Reference(typeof(Action))},
            {"System",                          Reference(typeof(Uri))},
            {"System.ComponentModel",           Reference(typeof(Component))},
            {"System.ComponentModel.Primitives",Reference(typeof(DescriptionAttribute))},
            {"System.Core",                     Reference(typeof(IQueryable))},
            {"System.Data",                     Reference(typeof(DataSet))},
            {"System.Xml",                      Reference(typeof(XmlElement))},
            {"System.Xml.Linq",                 Reference(typeof(XElement))},
            {"Microsoft.CSharp",                Reference(typeof(RuntimeBinderException))},
        };

        static readonly string[] DefaultNamespaces =
        {
            "Nake",
            "System",
            "System.Linq",
            "System.Text",
            "System.IO",
            "System.Collections.Generic", 
            "System.Threading.Tasks"
        };

        static MetadataReference Reference(Type type) => MetadataReference.CreateFromFile(type.Assembly.Location);

        readonly bool useRestoreCache;
        readonly Logger logger;

        readonly HashSet<string> namespaces;
        readonly List<MetadataReference> references;

        public Script(bool useRestoreCache, Logger logger)
        {
            this.useRestoreCache = useRestoreCache;
            this.logger = logger;

            namespaces = new HashSet<string>(DefaultNamespaces);
            references = new List<MetadataReference>(
                NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
        }

        public CompiledScript Compile(ScriptSource source)
        {
            if (source.IsFile)
                AddCompilationDependencies(source);

            var options = ScriptOptions.Default
                .AddImports(namespaces)
                .AddReferences(references)
                .WithMetadataResolver(new NuGetMetadataReferenceResolver(ScriptOptions.Default.MetadataResolver));

            if (source.IsFile)
                options = options.WithFilePath(source.File.FullName);

            var script = CSharpScript.Create(source.Code, options);
            var compilation = (CSharpCompilation)script.GetCompilation();

            var errors = compilation.GetDiagnostics()
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Any())   
                throw new ScriptCompilationException(errors);

            return new CompiledScript(references.Select(x => new AssemblyReference(x)), compilation);
        }

        void AddCompilationDependencies(ScriptSource source)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .Distinct()
                .ToDictionary(Path.GetFileName);

            var dependencyResolver = new CompilationDependencyResolver(t => logger);
            if (useRestoreCache)
            {
                var restorerField = dependencyResolver.GetType().GetField("_restorer", BindingFlags.Instance | BindingFlags.NonPublic);
                // ReSharper disable once PossibleNullReferenceException
                var currentRestorer = (IRestorer) restorerField.GetValue(dependencyResolver);
                restorerField.SetValue(dependencyResolver, new CachedRestorer(currentRestorer, t => logger));
            }

            var dependencies = dependencyResolver.GetDependencies(
                source.File.DirectoryName, 
                source.AllFiles().Select(x => x.File.ToString()), 
                true, "netcoreapp3.1");

            var assemblyReferences = dependencies
                .SelectMany(d => d.AssemblyPaths)
                .Select(l => loaded.TryGetValue(Path.GetFileName(l), out var e) ? e : l)
                .ToArray();

            foreach (var each in assemblyReferences)
                AddReference(each);
        }

        public void AddReference(string path) => AddReference(MetadataReference.CreateFromFile(path));
        
        void AddReference(MetadataReference reference)
        {
            if (references.Any(x => x == reference))
                return;
            
            references.Add(reference);
        }

        public void ImportNamespace(string ns)
        {
            namespaces.Add(ns);
        }
    }

    class CompiledScript
    {
        public readonly IEnumerable<AssemblyReference> References;
        public readonly CSharpCompilation Compilation;

        public CompiledScript(IEnumerable<AssemblyReference> references, CSharpCompilation compilation)
        {
            References = references;
            Compilation = compilation;
        }
    }
}