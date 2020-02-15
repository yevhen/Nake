using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Nake.Scripting
{
    class Script
    {
        static readonly List<MetadataReference> NakeReferences = new List<MetadataReference>
        {
            Reference(typeof(Engine)),
            Reference(typeof(TaskAttribute)),
            Reference(typeof(Env))
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
            {"Microsoft.Build.Framework",       Reference(typeof(ITaskItem))},
            {"Microsoft.Build.Utilities",       Reference(typeof(TaskItem))}
        };

        static readonly string[] DefaultNamespaces =
        {
            "Nake",
            "System",
            "System.Linq",
            "System.Text",
            "System.IO",
            "System.Collections.Generic", 
            "System.Threading.Tasks",
            "Microsoft.Build.Framework",
            "Microsoft.Build.Utilities"
        };

        static MetadataReference Reference(Type type) => MetadataReference.CreateFromFile(type.Assembly.Location);

        readonly HashSet<string> namespaces;
        readonly List<MetadataReference> resolved;
        readonly HashSet<string> unresolved;

        public Script()
        {
            namespaces = new HashSet<string>(DefaultNamespaces);
            resolved = new List<MetadataReference>(
                NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
            unresolved = new HashSet<string>();
        }

        public CompiledScript Compile(string code)
        {
            var options = ScriptOptions.Default
                .AddImports(namespaces)
                .AddReferences(resolved)
                .AddReferences(unresolved);

            var script = CSharpScript.Create(code, options);
            var compilation = (CSharpCompilation)script.GetCompilation();

            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                throw new NakeException("Script compilation failure! See diagnostics below." + Environment.NewLine +
                                        string.Join("\n", diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

            return new CompiledScript(resolved.Select(x => new AssemblyReference(x)), compilation);
        }

        public void AddReference(AssemblyNameReference reference)
        {
            if (DefaultReferences.ContainsKey(reference.AssemblyName))
                return;

            if (File.Exists(reference.FullPath))
            {
                AddReference(MetadataReference.CreateFromFile(reference.FullPath));
                return;
            }

            unresolved.Add(reference.AssemblyName);
        }

        public void AddReference(AssemblyAbsoluteReference reference)
        {
            if (!File.Exists(reference))
                throw new NakeException(
                    "Reference {0} defined in script {1} cannot be found",
                    reference.AssemblyPath, reference.ScriptFile);

            AddReference(MetadataReference.CreateFromFile(reference));
        }

        void AddReference(MetadataReference reference)
        {
            if (resolved.Any(x => x == reference))
                return;
            
            resolved.Add(reference);
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