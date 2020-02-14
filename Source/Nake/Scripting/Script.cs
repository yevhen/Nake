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
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
            {"System",                          Reference(typeof(Component))},
            {"System.Core",                     Reference(typeof(IQueryable))},
            {"System.Data",                     Reference(typeof(DataSet))},
            {"System.Xml",                      Reference(typeof(XmlElement))},
            {"System.Xml.Linq",                 Reference(typeof(XElement))},
            {"Microsoft.CSharp",                Reference(typeof(RuntimeBinderException))},
            {"Microsoft.Build.Framework",       Reference(typeof(ITaskItem))},
            {"Microsoft.Build.Utilities.v4.0",  Reference(typeof(TaskItem))}
        };

        static readonly string[] DefaultNamespaces =
        {
            "Nake",
            "System",
            "System.Linq",
            "System.Text",
            "System.IO",
            "System.Collections.Generic", 
            "Microsoft.Build.Framework",
            "Microsoft.Build.Utilities"
        };

        static MetadataReference Reference(Type type)
        {
            return MetadataReference.CreateFromFile(type.Assembly.Location);
        }

        readonly HashSet<string> namespaces;
        readonly List<MetadataReference> references;

        public Script()
        {
            namespaces = new HashSet<string>(DefaultNamespaces);
            references = new List<MetadataReference>(
                NakeReferences.Concat(DefaultReferences.Select(x => x.Value)));
        }

        public CompiledScript Compile(string code)
        {
            var options = ScriptOptions.Default
                .AddNamespaces(namespaces)
                .AddReferences(references);

            var script = CSharpScript.Create(code, options);
            var compilation = (CSharpCompilation)script.GetCompilation();

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

            AddReference(MetadataReference.CreateFromFile(fullPath));
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