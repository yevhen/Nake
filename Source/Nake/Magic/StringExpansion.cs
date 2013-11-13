using System;
using System.Linq;
using System.Text.RegularExpressions;

using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class StringExpansion
    {
        public static bool Qualifies(LiteralExpressionSyntax node)
        {
            return node.Kind == SyntaxKind.StringLiteralExpression;
        }

        static readonly Regex expressionPattern = new Regex(
            @"(?<!{){(?<expression>[^}{\r\n]+)}(?!})",
            RegexOptions.Compiled | RegexOptions.Singleline
        );

        static readonly Regex environmentVariablePattern = new Regex(
            @"(?<!\$)\$(?<variable>[^\${\r\n]+)\$(?!\$)",
            RegexOptions.Compiled | RegexOptions.Singleline
        );

        readonly SemanticModel model;
        readonly LiteralExpressionSyntax node;
        readonly bool constant;
        readonly string literal;

        public StringExpansion(SemanticModel model, LiteralExpressionSyntax node, bool constant)
        {
            this.model = model;
            this.node = node;
            this.constant = constant;
                
            literal  = node.Token.ValueText;
        }

        public SyntaxNode Expand()
        {                
            var expanded   = ExpandExpressions(literal);
            var inlined    = InlineEnvironmentVariables(expanded);
            var final      = Quote(Unescape(inlined));
                    
            return Syntax.ParseExpression(final);
        }

        string ExpandExpressions(string token)
        {
            if (constant)
                return token;

            return expressionPattern.Replace(token, match =>
            {
                var expression = match.Groups["expression"].Value;

                var syntax = Syntax.ParseExpression(expression);
                if (syntax.Span.Length != expression.Length)
                    throw new ExpressionSyntaxException(expression, LocationDiagnostics(match.Index));

                var type = model.GetSpeculativeTypeInfo(
                    node.FullSpan.End, syntax, 
                    SpeculativeBindingOption.BindAsExpression);

                if (type.Type.TypeKind == TypeKind.Error)
                    throw new ExpressionResolutionFailedException(expression, LocationDiagnostics(match.Index));
                
                if (type.Type.SpecialType == SpecialType.System_Void)
                    throw new ExpressionReturnTypeIsVoidException(expression, LocationDiagnostics(match.Index));

                return string.Format(@""" + ({0}) + @""", expression);
            });
        }

        string LocationDiagnostics(int matchPosition)
        {
            var span = node.GetLocation().GetLineSpan(true);

            return string.Format(
                "{0} ({1},{2})", 
                span.Path, 
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1 + matchPosition);
        }

        static string InlineEnvironmentVariables(string token)
        {
            return environmentVariablePattern.Replace(token, match =>
            {
                var variable = match.Groups["variable"].Value;
                var value = Env.Var[variable] ?? "$" + variable + "$";

                return Verbatimize(value);
            });
        }

        static string Quote(string token)
        {
            return string.Format(@"@""{0}""", token);
        }

        static string Verbatimize(string token)
        {
            return token.Replace("\"", "\"\"").Replace(@"\\", @"\");
        }

        string Unescape(string token)
        {
            var result = token.Replace("$$", "$");

            return !constant 
                    ? result.Replace("{{", "{").Replace("}}", "}") 
                    : result;
        }
    }
}