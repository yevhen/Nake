using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

using Nake.Magic;
using Nake.Scripting;

namespace Nake
{
    class BuildInput
    {
        AssemblyReference[] cached;

        public readonly ScriptSource Source;
        public readonly IDictionary<string, string> Substitutions;
        public readonly bool Debug;

        public BuildInput(ScriptSource source, IDictionary<string, string> substitutions, bool debug)
        {
            Source = source;
            Substitutions = substitutions;
            Debug = debug;
        }

        public BuildInput WithCached(AssemblyReference[] dependencies) => 
            new BuildInput(Source, Substitutions, Debug) {cached = dependencies};

        public IEnumerable<AssemblyReference> Dependencies() => 
            Source.ComputeDependencies(cached);
    }

    class BuildEngine
    {
        readonly IEnumerable<AssemblyReference> references;
        readonly IEnumerable<string> namespaces;

        public BuildEngine(
            IEnumerable<AssemblyReference> references = null,
            IEnumerable<string> namespaces = null)
        {
            this.references = references ?? Enumerable.Empty<AssemblyReference>();
            this.namespaces = namespaces ?? Enumerable.Empty<string>();
        }

        public BuildResult Build(BuildInput input)
        {
            var magic = new PixieDust(Compile(input.Source, input.Dependencies()));
            return magic.Apply(input.Substitutions, input.Debug);
        }

        CompiledScript Compile(ScriptSource source, IEnumerable<AssemblyReference> dependencies)
        {
            var script = new Script();

            foreach (var each in dependencies.Concat(references))
                script.AddReference(each);

            foreach (var each in namespaces)
                script.ImportNamespace(each);

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
            var rewrittenTrees = new Dictionary<string, CSharpSyntaxTree>();
            var capturedEnvironmentVariables = new HashSet<EnvironmentVariable>();
            var tasks = new List<Task>();
            
            foreach (CSharpSyntaxTree tree in script.Compilation.SyntaxTrees)
            {
                var analyzer = new Analyzer(substitutions, tree, script.Compilation.GetSemanticModel(tree, ignoreAccessibility: false));
                var analyzed = analyzer.Analyze();

                var rewriter = new Rewriter(analyzed, tree);
                rewrittenTrees.Add(tree.FilePath, rewriter.RewriteTree());

                Array.ForEach(rewriter.Captured.ToArray(), x => capturedEnvironmentVariables.Add(x));
                Array.ForEach(analyzed.Tasks.ToArray(), x => tasks.Add(x));
            }

            var rewrittenTree = rewrittenTrees.Count == 1
                ? rewrittenTrees.First().Value
                : rewrittenTrees[script.Source.File.FullName];

            var compilation = script.Compilation;
            if (script.Source.IsFile)
                compilation = compilation.WithOptions(compilation.Options.WithSourceReferenceResolver(LoadSourceResolver(rewrittenTrees)));

            compilation = compilation.ReplaceSyntaxTree(script.SyntaxTree, rewrittenTree);
            var rewritten = compilation;

            byte[] assembly;
            byte[] symbols = null;

            if (debug)
                EmitDebug(rewritten, out assembly, out symbols);
            else
                Emit(rewritten, out assembly);

            return new BuildResult(
                tasks.ToArray(), 
                script.References.ToArray(), 
                capturedEnvironmentVariables.ToArray(), 
                assembly, symbols);            
        }

        SourceFileResolver LoadSourceResolver(Dictionary<string, CSharpSyntaxTree> rewrittenTrees)
        {
            return new MySourceResolver(rewrittenTrees, script.Source.File.DirectoryName);
        }

        class MySourceResolver : SourceFileResolver
        {
            readonly Dictionary<string, CSharpSyntaxTree> rewrittenTrees;

            public MySourceResolver(Dictionary<string, CSharpSyntaxTree> rewrittenTrees, string baseDirectory)
                : base(ImmutableArray<string>.Empty, baseDirectory) =>
                this.rewrittenTrees = rewrittenTrees;

            public override SourceText ReadText(string resolvedPath) =>
                rewrittenTrees.ContainsKey(resolvedPath)
                    ? rewrittenTrees[resolvedPath].GetText()
                    : base.ReadText(resolvedPath);
        }

        void Emit(Compilation compilation, out byte[] assembly)
        {
            using var assemblyStream = new MemoryStream();
            
            Check(compilation, compilation.Emit(assemblyStream));
            
            assembly = assemblyStream.GetBuffer();
        }

        void EmitDebug(Compilation compilation, out byte[] assembly, out byte[] symbols)
        {
            using var assemblyStream = new MemoryStream();
            using var symbolStream = new MemoryStream();
            
            Check(compilation, compilation.Emit(assemblyStream, pdbStream: symbolStream));
            
            assembly = assemblyStream.GetBuffer();
            symbols = symbolStream.GetBuffer();
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
