using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    class AnalyzerResult : SyntaxWalker
    {
        readonly IDictionary<IMethodSymbol, Task> tasksBySymbol = new Dictionary<IMethodSymbol, Task>();
        readonly IDictionary<MethodDeclarationSyntax, Task> tasksBySyntax = new Dictionary<MethodDeclarationSyntax, Task>();
        readonly IDictionary<string, Task> tasksByName = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());
        
        readonly IDictionary<VariableDeclaratorSyntax, FieldSubstitution> substitutions = new Dictionary<VariableDeclaratorSyntax, FieldSubstitution>();
        readonly IDictionary<LiteralExpressionSyntax, StringInterpolation> interpolations = new Dictionary<LiteralExpressionSyntax, StringInterpolation>();        
        
        public IEnumerable<Task> Tasks => tasksBySymbol.Values;

        public Task Find(IMethodSymbol symbol) => tasksBySymbol.Find(symbol);
        public Task Find(MethodDeclarationSyntax method) => tasksBySyntax.Find(method);

        public void Add(IMethodSymbol symbol, Task task)
        {
            var existent = tasksByName.Find(task.FullName);
            if (existent != null)
                throw DuplicateTaskException.Create(existent, task);

            var method = (MethodDeclarationSyntax) symbol.DeclaringSyntaxReferences.First().GetSyntax();
            tasksBySyntax.Add(method, task);
            tasksBySymbol.Add(symbol, task);

            tasksByName.Add(task.FullName, task);
        }

        public void Add(VariableDeclaratorSyntax node, FieldSubstitution substitution) => substitutions.Add(node, substitution);
        public FieldSubstitution Find(VariableDeclaratorSyntax node) => substitutions.Find(node);

        public void Add(LiteralExpressionSyntax node, StringInterpolation interpolation) => interpolations.Add(node, interpolation);
        public StringInterpolation Find(LiteralExpressionSyntax node) => interpolations.Find(node);
    }
}