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

namespace Nake;

class BuildInput(ScriptSource source, IDictionary<string, string> substitutions, bool debug)
{
    AssemblyReference[] cached = [];

    public readonly ScriptSource Source = source;
    public readonly IDictionary<string, string> Substitutions = substitutions;
    public readonly bool Debug = debug;

    public BuildInput WithCached(AssemblyReference[] dependencies) => new(Source, Substitutions, Debug) {cached = dependencies};

    public IEnumerable<AssemblyReference> Dependencies() =>
        Source.ComputeDependencies(cached);
}

class BuildEngine(
    IEnumerable<AssemblyReference>? references = null,
    IEnumerable<string>? namespaces = null)
{
    readonly IEnumerable<AssemblyReference> references = references ?? [];
    readonly IEnumerable<string> namespaces = namespaces ?? [];

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

class PixieDust(CompiledScript script)
{
    public BuildResult Apply(IDictionary<string, string> substitutions, bool debug)
    {
        var result = Rewrite(substitutions);

        byte[] assembly;
        byte[]? symbols = null;

        if (debug)
            EmitDebug(result.Compilation, out assembly, out symbols);
        else
            Emit(result.Compilation, out assembly);

        return new BuildResult(
            result.Tasks,
            script.References.ToArray(),
            result.Captured,
            assembly, symbols);
    }

    RewriteResult Rewrite(IDictionary<string, string> substitutions)
    {
        var rewrittenTrees = new Dictionary<string, CSharpSyntaxTree>();
        var capturedEnvironmentVariables = new HashSet<EnvironmentVariable>();
        var tasks = new List<Task>();

        foreach (CSharpSyntaxTree tree in script.Compilation.SyntaxTrees)
        {
            var analyzed = Analyze(substitutions, tree);
            var captured = Rewrite(analyzed, tree, rewrittenTrees);

            Array.ForEach(captured.ToArray(), x => capturedEnvironmentVariables.Add(x));
            Array.ForEach(analyzed.Tasks.ToArray(), x => tasks.Add(x));
        }

        var rewrittenTree = rewrittenTrees.Count == 1
            ? rewrittenTrees.First().Value
            : rewrittenTrees[script.Source.File?.FullName ?? ""];

        var compilation = script.Compilation;
        if (script.Source.IsFile)
            compilation = compilation.WithOptions(compilation.Options.WithSourceReferenceResolver(Resolver(rewrittenTrees)));

        compilation = compilation.ReplaceSyntaxTree(script.SyntaxTree, rewrittenTree);
        var rewritten = compilation;

        return new RewriteResult(rewritten, tasks.ToArray(), capturedEnvironmentVariables.ToArray());
    }

    class RewriteResult(CSharpCompilation compilation, Task[] tasks, EnvironmentVariable[] captured)
    {
        public readonly CSharpCompilation Compilation = compilation;
        public readonly Task[] Tasks = tasks;
        public readonly EnvironmentVariable[] Captured = captured;
    }

    static HashSet<EnvironmentVariable> Rewrite(AnalyzerResult analyzed, CSharpSyntaxTree tree, Dictionary<string, CSharpSyntaxTree> rewrittenTrees)
    {
        var rewriter = new Rewriter(analyzed, tree);
        rewrittenTrees.Add(tree.FilePath, rewriter.Rewrite());
        return rewriter.Captured;
    }

    AnalyzerResult Analyze(IDictionary<string, string> substitutions, CSharpSyntaxTree tree)
    {
        var analyzer = new Analyzer(substitutions, tree, script.Compilation.GetSemanticModel(tree, ignoreAccessibility: false));
        return analyzer.Analyze();
    }

    SourceFileResolver Resolver(Dictionary<string, CSharpSyntaxTree> rewrittenTrees) =>
        new LoadScriptResolver(rewrittenTrees, script.Source.File?.DirectoryName ?? "");

    class LoadScriptResolver(Dictionary<string, CSharpSyntaxTree> rewrittenTrees, string baseDirectory)
        : SourceFileResolver(ImmutableArray<string>.Empty, baseDirectory)
    {
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

    void EmitDebug(Compilation compilation, out byte[] assembly, out byte[]? symbols)
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
    public readonly byte[]? SymbolBytes;

    public BuildResult(
        Task[] tasks,
        AssemblyReference[] references,
        EnvironmentVariable[] variables,
        byte[] assembly,
        byte[]? symbols)
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