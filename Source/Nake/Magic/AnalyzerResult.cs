using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic;

class AnalyzerResult : SyntaxWalker
{
#pragma warning disable RS1024 // Compare symbols correctly
    readonly Dictionary<IMethodSymbol, Task> tasksBySymbol = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
    readonly Dictionary<MethodDeclarationSyntax, Task> tasksBySyntax = new();
    readonly Dictionary<string, Task> tasksByName = new(new CaseInsensitiveEqualityComparer());
        
    readonly Dictionary<VariableDeclaratorSyntax, FieldSubstitution> substitutions = new();
        
    readonly Dictionary<CSharpSyntaxNode, IEnvironmentVariableInterpolation> interpolations = new();
        
    public IEnumerable<Task> Tasks => tasksBySymbol.Values;

    public Task? Find(IMethodSymbol symbol) => tasksBySymbol.Find(symbol);
    public Task? Find(MethodDeclarationSyntax method) => tasksBySyntax.Find(method);

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
    public FieldSubstitution? Find(VariableDeclaratorSyntax node) => substitutions.Find(node);

    public void Add(CSharpSyntaxNode node, IEnvironmentVariableInterpolation interpolation) => interpolations.Add(node, interpolation);
    public IEnvironmentVariableInterpolation? Find(CSharpSyntaxNode node) => interpolations.Find(node);
}