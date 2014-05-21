using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nake.Scripting
{
    class Submission
    {
        readonly HashSet<string> namespaces;
        readonly List<MetadataFileReference> references;

        public Submission(IEnumerable<string> defaultNamespaces, IEnumerable<MetadataFileReference> defaultReferences)
        {
            namespaces = new HashSet<string>(defaultNamespaces);
            references = new List<MetadataFileReference>(defaultReferences);
        }

        public void AddScriptDefinedAssemblyNameReference(AssemblyNameReference reference)
        {
            string fullPath;
            if (!GAC.AssemblyExist(reference.AssemblyName, out fullPath))
                throw new NakeException(
                    "Assembly reference {0} defined in script {1} cannot be found", 
                    reference.AssemblyName, reference.ScriptFile);

            AddScriptDefinedAssemblyReference(new MetadataFileReference(fullPath));
        }

        public void AddScriptDefinedAbsoluteReference(AbsoluteReference reference)
        {
            if (!File.Exists(reference))
                throw new NakeException(
                    "Reference {0} defined in script {1} cannot be found",
                    reference.AssemblyPath, reference.ScriptFile);

            AddScriptDefinedAssemblyReference(new MetadataFileReference(reference));
        }

        void AddScriptDefinedAssemblyReference(MetadataFileReference reference)
        {
            if (references.Any(x => x.FullPath == reference.FullPath))
                return;
            
            references.Add(reference);
        }

        public void ImportScriptDefinedNamespace(string ns)
        {
            namespaces.Add(ns);
        }

        public CSharpCompilation Compile(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code, 
                options: new CSharpParseOptions(kind: SourceCodeKind.Script));

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, usings: namespaces);

            var scriptingReference = BuildScriptingReference();

            var compilation = CSharpCompilation.CreateSubmission(
                Guid.NewGuid().ToString(), syntaxTree, 
                references.Concat(new[] {scriptingReference}), options);

            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                throw new ApplicationException("Script compilation failure! See diagnostics below." + Environment.NewLine +
                                               string.Join("\n", diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

            return compilation;
        }

        static MetadataReference BuildScriptingReference()
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Roslyn.Scripting.image";

            using (var stream = currentAssembly.GetManifestResourceStream(typeof(Script), resourceName))
            {
                Debug.Assert(stream != null);
                
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);

                return new MetadataImageReference(buffer, display: "Roslyn.Scripting");
            }
        }
    }
}