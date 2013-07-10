using System;
using System.Linq;

using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class FieldSubstitution
    {
        readonly VariableDeclaratorSyntax node;
        readonly FieldSymbol symbol;
        readonly string substitution;

        public FieldSubstitution(VariableDeclaratorSyntax node, FieldSymbol symbol, string substitution)
        {
            this.node = node;
            this.symbol = symbol;
            this.substitution = substitution;
        }

        public SyntaxNode Substitute()
        {
            var literal = TryCreateLiteral();

            if (literal != null)
                return node.WithInitializer(Syntax.EqualsValueClause(literal));

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

            return Syntax.LiteralExpression(kind)
                         .WithLeadingTrivia(new[]{Syntax.Space});            
        }

        LiteralExpressionSyntax IntegerLiteral()
        {
            int value;
            if (!int.TryParse(substitution, out value))
                return null;

            return Syntax.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression, 
                        Syntax.Literal(value))
                              .WithLeadingTrivia(new[]{Syntax.Space});            
        }

        LiteralExpressionSyntax StringLiteral()
        {
            return Syntax.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, 
                    Syntax.Literal(@"@""" + substitution + @"""", substitution)
                          .WithLeadingTrivia(new[]{Syntax.Space}));
        }

        public static bool Qualifies(FieldSymbol symbol)
        {
            var isAccessible =
                symbol.DeclaredAccessibility == Accessibility.Public &&
                (symbol.IsConst || symbol.IsStatic);

            var isTypeSupported = TypeConverter.IsSupported(symbol.Type);

            return isAccessible && isTypeSupported;
        }
    }
}