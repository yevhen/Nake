using System;
using System.Collections.Generic;
using System.Linq;

using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class AnalysisResult : SyntaxWalker
    {
        readonly IDictionary<MethodSymbol, Task> tasksBySymbol = new Dictionary<MethodSymbol, Task>();
        readonly IDictionary<string, Task> tasksByName = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());
        
        readonly IDictionary<InvocationExpressionSyntax, ProxyInvocation> invocations = new Dictionary<InvocationExpressionSyntax, ProxyInvocation>();
        readonly IDictionary<VariableDeclaratorSyntax, FieldSubstitution> substitutions = new Dictionary<VariableDeclaratorSyntax, FieldSubstitution>();
        readonly IDictionary<LiteralExpressionSyntax, StringExpansion> expansions = new Dictionary<LiteralExpressionSyntax, StringExpansion>();        
        
        public IEnumerable<Task> Tasks
        {
            get { return tasksBySymbol.Values; }
        }

        public Task Find(MethodSymbol symbol)
        {
            return tasksBySymbol.Find(symbol);
        }

        public void Add(MethodSymbol symbol, Task task)
        {
            var existent = tasksByName.Find(task.FullName);
            if (existent != null)
                throw DuplicateTaskException.Create(existent, task);

            tasksBySymbol.Add(symbol, task);
            tasksByName.Add(task.FullName, task);
        }

        public void Add(InvocationExpressionSyntax node, ProxyInvocation invocation)
        {
            invocations.Add(node, invocation);
        }

        public ProxyInvocation Find(InvocationExpressionSyntax node)
        {
            return invocations.Find(node);
        }

        public void Add(VariableDeclaratorSyntax node, FieldSubstitution substitution)
        {
            substitutions.Add(node, substitution);
        }

        public FieldSubstitution Find(VariableDeclaratorSyntax node)
        {
            return substitutions.Find(node);
        }

        public void Add(LiteralExpressionSyntax node, StringExpansion expansion)
        {
            expansions.Add(node, expansion);
        }
        
        public StringExpansion Find(LiteralExpressionSyntax node)
        {
            return expansions.Find(node);
        }
    }
}