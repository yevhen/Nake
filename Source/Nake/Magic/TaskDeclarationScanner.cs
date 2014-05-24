using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    class TaskDeclarationScanner : CSharpSyntaxWalker
    {
        readonly Dictionary<string, TaskDeclaration> tasks = 
             new Dictionary<string, TaskDeclaration>(new CaseInsensitiveEqualityComparer());

        readonly Stack<string> scope = new Stack<string>();

        public TaskDeclarationScanner()
            : base(SyntaxWalkerDepth.Node)
        {}

        public IEnumerable<TaskDeclaration> Scan(string code, bool comments = true)
        {
            var tree = CSharpSyntaxTree.ParseText(code,
                new CSharpParseOptions(kind: SourceCodeKind.Script, 
                documentationMode: comments ? DocumentationMode.Parse : DocumentationMode.None));

            var diagnostics = tree.GetDiagnostics().ToArray();
            if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                throw new NakeException("Script parsing failure! See diagnostics below." + Environment.NewLine +
                                         string.Join("\n", diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

            ((CSharpSyntaxTree)tree).GetRoot().Accept(this);
            return tasks.Values;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            scope.Push(node.Identifier.ToString());
            base.VisitClassDeclaration(node);
            scope.Pop();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!TaskDeclaration.IsAnnotated(node))
                return;
            
            var task = new TaskDeclaration(string.Join(".", scope.Reverse()), node);
            if (tasks.ContainsKey(task.FullName))
                throw DuplicateTaskException.Create(tasks[task.FullName], task);

            tasks.Add(task.FullName, task);
        }
    }
}