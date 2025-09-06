﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nake.Utility;

namespace Nake.Magic;

class FieldSubstitution(VariableDeclaratorSyntax node, IFieldSymbol symbol, string substitution)
{
    public SyntaxNode Substitute()
    {
        var literal = TryCreateLiteral();

        if (literal != null)
            return node.WithInitializer(SyntaxFactory.EqualsValueClause(literal));

        Log.Trace($"Matched field {symbol} with substitution coming from cmd line but type conversion failed");

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
            .WithLeadingTrivia(SyntaxFactory.Space);
    }

    LiteralExpressionSyntax IntegerLiteral()
    {
        int value;
        if (!int.TryParse(substitution, out value))
            return null;

        return SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(value))
            .WithLeadingTrivia(SyntaxFactory.Space);
    }

    LiteralExpressionSyntax StringLiteral()
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(@"@""" + substitution + @"""", substitution)
                .WithLeadingTrivia(SyntaxFactory.Space));
    }

    public static bool Qualifies(IFieldSymbol symbol) => TypeConverter.IsSupported(symbol.Type);
}