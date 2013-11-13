using System;
using System.Collections.Generic;
using System.Linq;

using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class Rewriter : SyntaxRewriter
    {
        readonly SemanticModel model;
        readonly AnalysisResult result;
        
        readonly HashSet<LiteralExpressionSyntax> skip  = new HashSet<LiteralExpressionSyntax>();

        public Rewriter(SemanticModel model, AnalysisResult result)
        {
            this.model = model;
            this.result = result;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            SkipLiteralsWithinStringFormat(node);

            var invocation = result.Find(node);
            if (invocation == null)
                return base.VisitInvocationExpression(node);

            var replacement = invocation.Replace();                
            ReprocessStringLiterals(replacement);

            return base.Visit(replacement);
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (ShouldSkip(node))
                return base.VisitLiteralExpression(node);

            var expansion = result.Find(node);

            return expansion != null 
                             ? expansion.Expand() 
                             : base.VisitLiteralExpression(node);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var substitution = result.Find(node);
    
            return substitution != null 
                                ? substitution.Substitute() 
                                : base.VisitVariableDeclarator(node);
        }

        bool ShouldSkip(LiteralExpressionSyntax node)
        {
            return skip.Contains(node);
        }

        void SkipLiteralsWithinStringFormat(ExpressionSyntax node)
        {
            var symbol = model.GetSpeculativeSymbolInfo(node.Span.Start, node,
                SpeculativeBindingOption.BindAsExpression).Symbol as MethodSymbol;

            if (symbol == null || (!symbol.ToString().StartsWith("string.Format(")))
                return;

            foreach (var literal in GetExpansionQualifiedLiterals(node))
            {
                skip.Add(literal);
            }
        }

        void ReprocessStringLiterals(SyntaxNode node)
        {
            foreach (var literal in GetExpansionQualifiedLiterals(node))
            {
                result.Add(literal, new StringExpansion(model, literal, false));
            }
        }

        public static IEnumerable<LiteralExpressionSyntax> GetExpansionQualifiedLiterals(SyntaxNode node)
        {
            return node.DescendantNodes() 
                       .OfType<LiteralExpressionSyntax>()
                       .Where(StringExpansion.Qualifies);
        }
    }
}