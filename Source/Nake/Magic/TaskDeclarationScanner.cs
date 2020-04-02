using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nake.Scripting;

namespace Nake.Magic
{
    class TaskDeclarationScanner : CSharpSyntaxWalker
    {
        public static TaskDeclaration[] Scan(ScriptSource source, bool comments = true)
        {
            var result = new HashSet<TaskDeclaration>();
            
            foreach (var each in source.AllFiles())
            {
                new TaskDeclarationScanner()
                    .Scan(each.Code, comments).ToList()
                    .ForEach(x => result.Add(x));
            }

            return result.ToArray();
        }

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
            var isTask = IsTask(node);
            var isStep = IsStep(node);

            if (!isTask && !isStep)
                return;
            
            var task = new TaskDeclaration(string.Join(".", scope.Reverse()), node, isStep);
            if (tasks.ContainsKey(task.FullName))
                throw DuplicateTaskException.Create(tasks[task.FullName], task);

            tasks.Add(task.FullName, task);
        }

        static bool IsTask(MethodDeclarationSyntax node)
        {
            return node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Task"));
        }

        static bool IsStep(MethodDeclarationSyntax node)
        {
            return node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Step"));
        }
    }
}