using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
	class Rewriter : CSharpSyntaxRewriter
	{
		public readonly HashSet<EnvironmentVariable> Captured = new HashSet<EnvironmentVariable>();

		readonly CSharpSyntaxTree tree;
		readonly SemanticModel model;
		readonly CSharpCompilation compilation;
		readonly AnalyzerResult result;

		public Rewriter(CSharpCompilation compilation, AnalyzerResult result)
		{
			tree = (CSharpSyntaxTree)compilation.SyntaxTrees.Single();
			model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

			this.compilation = compilation;
			this.result = result;
		}

		public CSharpCompilation Rewrite()
		{
			var newRoot = tree.GetRoot().Accept(this);
			return compilation.ReplaceSyntaxTree(tree, CreateTree(newRoot));
		}

		SyntaxTree CreateTree(SyntaxNode root)
		{
			var options = new CSharpParseOptions(
				documentationMode: DocumentationMode.None,
				kind: SourceCodeKind.Script);

			return CSharpSyntaxTree.Create((CompilationUnitSyntax)root, options, tree.FilePath, Encoding.UTF8);
		}

		public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var visited = (InvocationExpressionSyntax)
				base.VisitInvocationExpression(node);

			var invocation = result.Find(node);
			return invocation != null
							  ? invocation.Replace(visited)
							  : visited;
		}

		public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			var interpolation = result.Find(node);
			if (interpolation == null)
				return base.VisitLiteralExpression(node);

			var interpolated = interpolation.Interpolate();
			foreach (var variable in interpolation.Captured)
				Captured.Add(variable);

			return interpolated;
		}

		public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
		{
			var substitution = result.Find(node);

			return substitution != null
								? substitution.Substitute()
								: base.VisitVariableDeclarator(node);
		}
	}
}