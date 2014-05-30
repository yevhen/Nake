using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.RuntimeBinder;

namespace Nake.Scripting
{
    class Script
    {
        static readonly List<MetadataFileReference> NakeReferences = new List<MetadataFileReference>
        {
            Reference(typeof(Engine)),
            Reference(typeof(TaskAttribute)),
            Reference(typeof(Env))
        };

        static readonly Dictionary<string, MetadataFileReference> DefaultReferences = new Dictionary<string, MetadataFileReference>
        {
            {"mscorlib",                        Reference(typeof(object))},
            {"System",                          Reference(typeof(Component))},
            {"System.Core",                     Reference(typeof(IQueryable))},
            {"System.Data",                     Reference(typeof(DataSet))},
            {"System.Data.DataSetExtensions",   Reference(typeof(DataTableExtensions))},
            {"System.Xml",                      Reference(typeof(XmlElement))},
            {"System.Xml.Linq",                 Reference(typeof(XElement))},
            {"Microsoft.CSharp",                Reference(typeof(RuntimeBinderException))},
        };

        static readonly string[] DefaultNamespaces =
        {
            "Nake", "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO"
        };

        static MetadataFileReference Reference(Type type)
        {
            return new MetadataFileReference(type.Assembly.Location);
        }

        readonly HashSet<string> namespaces;
        readonly List<MetadataFileReference> references;

        public Script()
        {
            namespaces = new HashSet<string>(DefaultNamespaces);
            references = new List<MetadataFileReference>(
                NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
        }

        public CompiledScript Compile(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code, 
                new CSharpParseOptions(kind: SourceCodeKind.Script), 
                encoding: Encoding.UTF8
            );

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, usings: namespaces
            );

            var compilation = CSharpCompilation.CreateSubmission(
                Guid.NewGuid().ToString(), syntaxTree, 
                references.Concat(new[] {RoslynScriptingAssembly.BuildReference()}), options
            );

            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                throw new NakeException("Script compilation failure! See diagnostics below." + Environment.NewLine +
                                        string.Join("\n", diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

            return new CompiledScript(references.Select(x => new AssemblyReference(x)), compilation);
        }

        public void AddReference(AssemblyNameReference reference)
        {
            if (DefaultReferences.ContainsKey(reference.AssemblyName))
                return;

            string fullPath;
            if (!GAC.AssemblyExist(reference.AssemblyName, out fullPath))
                throw new NakeException(
                    "Assembly reference {0} defined in script {1} cannot be found", 
                    reference.AssemblyName, reference.ScriptFile);

            AddReference(new MetadataFileReference(fullPath));
        }

        public void AddReference(AssemblyAbsoluteReference reference)
        {
            if (!File.Exists(reference))
                throw new NakeException(
                    "Reference {0} defined in script {1} cannot be found",
                    reference.AssemblyPath, reference.ScriptFile);

            AddReference(new MetadataFileReference(reference));
        }

        void AddReference(MetadataFileReference reference)
        {
            if (references.Any(x => x.FullPath == reference.FullPath))
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