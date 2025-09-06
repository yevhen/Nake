﻿using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic;

class Rewriter(AnalyzerResult result, CSharpSyntaxTree tree) : CSharpSyntaxRewriter
{
    public readonly HashSet<EnvironmentVariable> Captured = new();

    public CSharpSyntaxTree Rewrite()
    {
        var newRoot = tree.GetRoot().Accept(this)!;
        return CreateTree(newRoot);
    }

    CSharpSyntaxTree CreateTree(SyntaxNode root)
    {
        var options = new CSharpParseOptions(
            documentationMode: DocumentationMode.None,
            kind: SourceCodeKind.Script);

        return (CSharpSyntaxTree) CSharpSyntaxTree.Create((CompilationUnitSyntax) root, options, tree.FilePath, Encoding.UTF8);
    }

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)
            base.VisitMethodDeclaration(node)!;

        var task = result.Find(node);
        return task?.Replace(visited) ?? visited;
    }

    public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        var substitution = result.Find(node);

        return substitution?.Substitute() ?? base.VisitVariableDeclarator(node)!;
    }

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        var interpolation = result.Find(node);
        if (interpolation == null)
            return base.VisitLiteralExpression(node)!;

        var (interpolated, captured) = interpolation.Interpolate();
        foreach (var variable in captured)
            Captured.Add(variable);

        return interpolated;
    }

    public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
    {
        var interpolation = result.Find(node);
        return interpolation != null
            ? (interpolation.Interpolate()).interpolated
            : base.VisitInterpolatedStringExpression(node)!;
    }
}