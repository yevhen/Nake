using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class FairyDust
    {
        readonly Compilation compilation;
        readonly IDictionary<string, string> substitutions;
        readonly bool debug;

        readonly SyntaxTree originalTree;
        readonly SemanticModel semanticModel;

        public FairyDust(Compilation compilation, IDictionary<string, string> substitutions, bool debug)
        {
            this.compilation = compilation;
            this.substitutions = substitutions;
            this.debug = debug;

            originalTree = this.compilation.SyntaxTrees.Single();
            semanticModel = this.compilation.GetSemanticModel(originalTree);
        }

        public IEnumerable<Task> Apply()
        {
            var result = Analyze();

            var assembly = Emit(Rewrite(result), debug);
            ReflectTasks(result.Tasks, assembly);

            return result.Tasks;
        }

        AnalysisResult Analyze()
        {
            var analyzer = new Analyzer(semanticModel, substitutions);
            analyzer.Visit(originalTree.GetRoot());

            return analyzer.Result;
        }

        Compilation Rewrite(AnalysisResult result)
        {
            var rewriter = new Rewriter(semanticModel, result);
            var newRoot = rewriter.Visit(originalTree.GetRoot());
            
            var rewrittenTree = SyntaxTree.Create((CompilationUnitSyntax) newRoot,
                originalTree.FilePath, originalTree.Options);

            return compilation.ReplaceSyntaxTree(originalTree, rewrittenTree);
        }

        static Assembly Emit(Compilation compilation, bool debug)
        {
            return debug ? EmitWithPdb(compilation) : JustEmit(compilation);
        }

        static Assembly EmitWithPdb(Compilation compilation)
        {
            Assembly result;

            using (var outputStream = new MemoryStream())
            using (var symbolStream = new MemoryStream()) 
            {
                Check(compilation.Emit(outputStream, pdbStream: symbolStream));
                result = Assembly.Load(outputStream.GetBuffer(), symbolStream.GetBuffer());
            }

            return result;
        }

        static Assembly JustEmit(Compilation compilation)
        {
            using (var outputStream = new MemoryStream())
            {
                Check(compilation.Emit(outputStream));
                return Assembly.Load(outputStream.GetBuffer());
            }
        }

        static void Check(EmitResult result)
        {
            if (result.Success)
                return;

            var errors = result.Diagnostics
                .Where(d => d.Info.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Any())
                throw new NakeException("Compilation failed!\r\n\r\n" +
                                        string.Join("\r\n", errors.Select(x => x.ToString())));
        }

        static void ReflectTasks(IEnumerable<Task> tasks, Assembly assembly)
        {
            foreach (var task in tasks)
            {
                task.Reflect(assembly);
            }
        }
    }
}