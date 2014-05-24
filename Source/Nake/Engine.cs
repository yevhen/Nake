using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
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

        public BuildOutput Build(string code, IDictionary<string, string> substitutions, bool debug)
        {
            var magic = new PixieDust(Compile(code));
            return magic.Apply(substitutions, debug);
        }

        ScriptCompilationOutput Compile(string code)
        {
            var script = new Script();

            foreach (var reference in assemblyNameReferences)
                script.AddReference(reference);

            foreach (var reference in assemblyAbsoluteReferences)
                script.AddReference(reference);

            foreach (var @namespace in namespaces)
                script.ImportNamespace(@namespace);

            return script.Compile(code);
        }
    }

    class PixieDust
    {
        readonly ScriptCompilationOutput output;

        public PixieDust(ScriptCompilationOutput output)
        {
            this.output = output;
        }

        public BuildOutput Apply(IDictionary<string, string> substitutions, bool debug)
        {
            var analyzer = new Analyzer(output.Compilation, substitutions);
            var analyzed = analyzer.Analyze();

            var rewriter = new Rewriter(output.Compilation, analyzed);
            var rewritten = rewriter.Rewrite();

            byte[] assembly;
            byte[] symbols = null;

            if (debug)
                EmitDebug(rewritten, out assembly, out symbols);
            else
                Emit(rewritten, out assembly);

            return new BuildOutput(
                analyzed.Tasks.ToArray(), 
                output.References.ToArray(), 
                rewriter.Captured.ToArray(), 
                assembly, symbols
            );            
        }

        static void Emit(Compilation compilation, out byte[] assembly)
        {
            using (var assemblyStream = new MemoryStream())
            {
                Check(compilation.Emit(assemblyStream));
                assembly = assemblyStream.GetBuffer();
            }
        }

        static void EmitDebug(Compilation compilation, out byte[] assembly, out byte[] symbols)
        {
            using (var assemblyStream = new MemoryStream())
            using (var symbolStream = new MemoryStream())
            {
                Check(compilation.Emit(assemblyStream, pdbStream: symbolStream));

                assembly = assemblyStream.GetBuffer();
                symbols = symbolStream.GetBuffer();
            }
        }

        static void Check(EmitResult result)
        {
            if (result.Success)
                return;

            var errors = result.Diagnostics
                .WhereAsArray(x => x.Severity == DiagnosticSeverity.Error);

            if (errors.Any())
                throw new NakeException("Compilation failed!\r\n\r\n" +
                    string.Join("\r\n", errors.Select(x => x.ToString())));
        }
    }

    class BuildOutput
    {
        public readonly Task[] Tasks;
        public readonly AssemblyReference[] References;
        public readonly EnvironmentVariable[] Variables;
        public readonly byte[] Assembly;
        public readonly byte[] Symbols;

        public BuildOutput(
            Task[] tasks,
            AssemblyReference[] references,
            EnvironmentVariable[] variables,
            byte[] assembly,
            byte[] symbols)
        {
            Tasks = tasks;
            References = references;
            Assembly = assembly;
            Symbols = symbols;
            Variables = variables;
            Reflect(tasks);
        }

        void Reflect(IEnumerable<Task> tasks)
        {
            AssemblyResolver.Register();

            foreach (var reference in References)
                AssemblyResolver.Add(reference);

            var assembly = Symbols != null
                ? System.Reflection.Assembly.Load(Assembly, Symbols)
                : System.Reflection.Assembly.Load(Assembly);

            foreach (var task in tasks)
                task.Reflect(assembly);
        }
    }
}
