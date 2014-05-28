using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    class FieldSubstitution
    {
        readonly VariableDeclaratorSyntax node;
        readonly IFieldSymbol symbol;
        readonly string substitution;

        public FieldSubstitution(VariableDeclaratorSyntax node, IFieldSymbol symbol, string substitution)
        {
            this.node = node;
            this.symbol = symbol;
            this.substitution = substitution;
        }

        public SyntaxNode Substitute()
        {
            var literal = TryCreateLiteral();

            if (literal != null)
                return node.WithInitializer(SyntaxFactory.EqualsValueClause(literal));

            Log.Trace(string.Format("Matched field {0} with substitution coming from cmd line but type conversion failed", symbol));

            return node;
        }

        LiteralExpressionSyntax TryCreateLiteral()
        {
            if (symbol.Type.IsBoolean())
                return BooleanLiteral();

            if (symbol.Type.IsInteger())
                return IntegerLiteral();

            if (symbol.Type.IsString())
                return StringLiteral();

            throw new NakeException("Unsupported literal type " + symbol.Type);
        }

        LiteralExpressionSyntax BooleanLiteral()
        {
            bool value;
            if (!bool.TryParse(substitution, out value))
                return null;

            var kind = value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;

            return SyntaxFactory.LiteralExpression(kind)
                         .WithLeadingTrivia(new[] {SyntaxFactory.Space});            
        }

        LiteralExpressionSyntax IntegerLiteral()
        {
            int value;
            if (!int.TryParse(substitution, out value))
                return null;

            return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(value))
                              .WithLeadingTrivia(new[] {SyntaxFactory.Space});            
        }

        LiteralExpressionSyntax StringLiteral()
        {
            return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(@"@""" + substitution + @"""", substitution)
                          .WithLeadingTrivia(new[] {SyntaxFactory.Space}));
        }

        public static bool Qualifies(IFieldSymbol symbol)
        {
            return TypeConverter.IsSupported(symbol.Type);
        }
    }
}