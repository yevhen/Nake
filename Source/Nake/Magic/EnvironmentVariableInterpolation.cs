using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic;

interface IEnvironmentVariableInterpolation
{
    (SyntaxNode interpolated, EnvironmentVariable[] captured) Interpolate();
}

class EnvironmentVariableInterpolation
{
    const string NakeScriptDirectoryVariable = "NakeScriptDirectory";

    public static IEnvironmentVariableInterpolation Match(SemanticModel model, LiteralExpressionSyntax node, bool constant)
    {
        if (node.Kind() != SyntaxKind.StringLiteralExpression)
            return null;

        var filePath = ScriptFilePath(model, node);

        if (constant && Constant.Qualifies(node))
            return new Constant(node, filePath);

        return RuntimeWithinLiteral.Qualifies(node)
            ? new RuntimeWithinLiteral(node, filePath)
            : null;
    }

    public static IEnvironmentVariableInterpolation Match(SemanticModel model, InterpolatedStringExpressionSyntax node) =>
        RuntimeWithinInterpolation.Qualifies(node)
            ? new RuntimeWithinInterpolation(node, ScriptFilePath(model, node))
            : null;

    static string ScriptFilePath(SemanticModel model, SyntaxNode node)
    {
        var symbol = model.GetEnclosingSymbol(node.FullSpan.Start);
        return (symbol.Locations.FirstOrDefault()?.GetLineSpan())?.Path ?? "NOT_A_FILE";
    }

    static bool Qualifies(string text) =>
        VariablePattern.IsMatch(text) ||
        EscapePattern.IsMatch(text);

    static readonly Regex VariablePattern = new(
        @"(?<!\%)\%(?<variable>[^\%{\r\n]+)\%(?!\%)",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    static readonly Regex EscapePattern = new(
        @"\%\%(?<escape>[^\%{\r\n]+)\%\%",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    class Constant(LiteralExpressionSyntax node, string filePath) : IEnvironmentVariableInterpolation
    {
        public static bool Qualifies(LiteralExpressionSyntax node) =>
            EnvironmentVariableInterpolation.Qualifies(node.ToString());

        public (SyntaxNode, EnvironmentVariable[]) Interpolate()
        {
            var captured = new HashSet<EnvironmentVariable>();

            var literal = node.Token.ValueText;
            var inlined = VariablePattern.Replace(literal, match =>
            {
                var name  = match.Groups["variable"].Value;
                if (name == NakeScriptDirectoryVariable)
                    return Path.GetDirectoryName(filePath);

                var value = Environment.GetEnvironmentVariable(name) ?? "";
                captured.Add(new EnvironmentVariable(name, value));

                return value;
            });

            var interpolated = node.WithToken(SyntaxFactory.Literal(Unescape(inlined)));
            return (interpolated, captured.ToArray());
        }
    }

    class RuntimeWithinLiteral(LiteralExpressionSyntax node, string filePath) : IEnvironmentVariableInterpolation
    {
        public static bool Qualifies(LiteralExpressionSyntax node) =>
            EnvironmentVariableInterpolation.Qualifies(node.ToString());

        public (SyntaxNode, EnvironmentVariable[]) Interpolate()
        {
            var contents = InterpolateContents(node.Token.ValueText, filePath);

            var interpolated = SyntaxFactory.InterpolatedStringExpression(
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                SyntaxFactory.List(contents),
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));

            return (interpolated, []);
        }
    }

    class RuntimeWithinInterpolation(InterpolatedStringExpressionSyntax node, string filePath)
        : IEnvironmentVariableInterpolation
    {
        public static bool Qualifies(InterpolatedStringExpressionSyntax node) =>
            node.Contents.OfType<InterpolatedStringTextSyntax>().Any(Qualifies);

        static bool Qualifies(InterpolatedStringTextSyntax node) =>
            EnvironmentVariableInterpolation.Qualifies(node.TextToken.ValueText);

        public (SyntaxNode, EnvironmentVariable[]) Interpolate()
        {
            var result = new List<InterpolatedStringContentSyntax>();

            foreach (var each in node.Contents)
            {
                if (each is InterpolatedStringTextSyntax its && Qualifies(its))
                {
                    result.AddRange(InterpolateContents(its.TextToken.ValueText, filePath));
                    continue;
                }

                result.Add(each);
            }

            var interpolated = SyntaxFactory.InterpolatedStringExpression(
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                SyntaxFactory.List(result),
                SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));

            return (interpolated, []);
        }
    }

    static IEnumerable<InterpolatedStringContentSyntax> InterpolateContents(string literal, string filePath)
    {
        var start = 0;

        var match = VariablePattern.Match(literal);
        while (match.Success)
        {
            var text = literal.Substring(start, match.Index - start);
            if (!string.IsNullOrEmpty(text))
                yield return InterpolatedStringText(Unescape(text));

            var name = match.Groups["variable"].Value;
            if (name == NakeScriptDirectoryVariable)
            {
                var inline = $"@\"{Path.GetDirectoryName(filePath)}\"";
                yield return SyntaxFactory.Interpolation(SyntaxFactory.ParseExpression(inline));
            }
            else
            {
                var invocation = $"{typeof(Substitutions).FullName}.{nameof(Substitutions.EnvironmentVariable)}(\"{name}\")";
                yield return SyntaxFactory.Interpolation(SyntaxFactory.ParseExpression(invocation));
            }

            start = match.Index + match.Length;
            match = match.NextMatch();
        }

        var tail = literal.Substring(start);
        if (!string.IsNullOrEmpty(tail))
            yield return InterpolatedStringText(Unescape(tail));
    }

    static InterpolatedStringTextSyntax InterpolatedStringText(string text) =>
        SyntaxFactory.InterpolatedStringText(
            SyntaxFactory.Token(
                SyntaxTriviaList.Empty,
                SyntaxKind.InterpolatedStringTextToken,
                DisplayFormat(text),
                text,
                SyntaxTriviaList.Empty));

    static string DisplayFormat(string text)
    {
        return Unquote(Format(text));

        static string Format(string s) => SymbolDisplay.FormatLiteral(s, quote: true);
        static string Unquote(string s) => s.Remove(s.Length - 1, 1).Remove(0, 1);
    }

    static string Unescape(string text) => EscapePattern.Replace(text, match => $"%{match.Groups["escape"].Value}%");
}