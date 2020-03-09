using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Nake.Magic;
using Nake.Scripting;

namespace Nake
{
    class Engine
    {
        readonly IEnumerable<AssemblyNameReference> assemblyNameReferences;
        readonly IEnumerable<AssemblyAbsoluteReference> assemblyAbsoluteReferences;
        readonly IEnumerable<string> namespaces;

        public Engine(
            IEnumerable<AssemblyNameReference> assemblyNameReferences = null,
            IEnumerable<AssemblyAbsoluteReference> assemblyAbsoluteReferences = null,
            IEnumerable<string> namespaces = null)
        {
            this.assemblyNameReferences = assemblyNameReferences ?? Enumerable.Empty<AssemblyNameReference>();
            this.assemblyAbsoluteReferences = assemblyAbsoluteReferences ?? Enumerable.Empty<AssemblyAbsoluteReference>();
            this.namespaces = namespaces ?? Enumerable.Empty<string>();
        }

        public BuildResult Build(ScriptSource source, IDictionary<string, string> substitutions, bool debug)
        {
            var magic = new PixieDust(Compile(source));
            return magic.Apply(substitutions, debug);
        }

        CompiledScript Compile(ScriptSource source)
        {
            var script = new Script();

            foreach (var reference in assemblyNameReferences)
                script.AddReference(reference);

            foreach (var reference in assemblyAbsoluteReferences)
                script.AddReference(reference);

            foreach (var @namespace in namespaces)
                script.ImportNamespace(@namespace);

            return script.Compile(source);
        }
    }

    class PixieDust
    {
        readonly CompiledScript script;

        public PixieDust(CompiledScript script)
        {
            this.script = script;
        }

        public BuildResult Apply(IDictionary<string, string> substitutions, bool debug)
        {
            var analyzer = new Analyzer(script.Compilation, substitutions);
            var analyzed = analyzer.Analyze();

            var rewriter = new Rewriter(script.Compilation, analyzed);
            var rewritten = rewriter.Rewrite();

            byte[] assembly;
            byte[] symbols = null;

            if (debug)
                EmitDebug(rewritten, out assembly, out symbols);
            else
                Emit(rewritten, out assembly);

            return new BuildResult(
                analyzed.Tasks.ToArray(), 
                script.References.ToArray(), 
                rewriter.Captured.ToArray(), 
                assembly, symbols
            );            
        }

        void Emit(Compilation compilation, out byte[] assembly)
        {
            using (var assemblyStream = new MemoryStream())
            {
                Check(compilation, compilation.Emit(assemblyStream));
                assembly = assemblyStream.GetBuffer();
            }
        }

        void EmitDebug(Compilation compilation, out byte[] assembly, out byte[] symbols)
        {
            using (var assemblyStream = new MemoryStream())
            using (var symbolStream = new MemoryStream())
            {
                Check(compilation, compilation.Emit(assemblyStream, pdbStream: symbolStream));

                assembly = assemblyStream.GetBuffer();
                symbols = symbolStream.GetBuffer();
            }
        }

        void Check(Compilation compilation, EmitResult result)
        {
            if (result.Success)
                return;

            var errors = result.Diagnostics
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Any())
                throw new RewrittenScriptCompilationException(SourceText(script.Compilation), SourceText(compilation), errors);

            static string SourceText(Compilation arg) => arg.SyntaxTrees.First().ToString();
        }
    }

    class BuildResult
    {
        public readonly Task[] Tasks;
        public readonly AssemblyReference[] References;
        public readonly EnvironmentVariable[] Variables;
        public readonly Assembly Assembly;
        public readonly byte[] AssemblyBytes;
        public readonly byte[] SymbolBytes;

        public BuildResult(
            Task[] tasks,
            AssemblyReference[] references,
            EnvironmentVariable[] variables,
            byte[] assembly,
            byte[] symbols)
        {
            Tasks = tasks;
            References = references;
            AssemblyBytes = assembly;
            SymbolBytes = symbols;
            Variables = variables;
            Assembly = Load();
            Reflect();
        }

        Assembly Load()
        {
            AssemblyResolver.Register();

            foreach (var reference in References)
                AssemblyResolver.Add(reference);

            return SymbolBytes != null
                       ? Assembly.Load(AssemblyBytes, SymbolBytes)
                       : Assembly.Load(AssemblyBytes);
        }

        void Reflect()
        {
            foreach (var task in Tasks)
                task.Reflect(Assembly);
        }
    }
}
