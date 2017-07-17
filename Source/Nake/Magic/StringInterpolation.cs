﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nake.Utility;

namespace Nake.Magic
{
	class StringInterpolation
	{
		public readonly HashSet<EnvironmentVariable> Captured = new HashSet<EnvironmentVariable>();

		public static bool Qualifies(LiteralExpressionSyntax node)
		{
			return node.Kind() == SyntaxKind.StringLiteralExpression;
		}

		static readonly Regex expressionPattern = new Regex(
			@"\$(?<!{){(?<expression>[^}{\r\n]+)}(?!})",
			RegexOptions.Compiled | RegexOptions.Singleline
		);

		static readonly Regex environmentVariablePattern = new Regex(
			@"(?<!\%)\%(?<variable>[^\%{\r\n]+)\%(?!\%)",
			RegexOptions.Compiled | RegexOptions.Singleline
		);

		readonly SemanticModel model;
		readonly LiteralExpressionSyntax node;
		readonly bool constant;
		readonly string literal;

		public StringInterpolation(SemanticModel model, LiteralExpressionSyntax node, bool constant)
		{
			this.model = model;
			this.node = node;
			this.constant = constant;

			literal = node.Token.ValueText;
		}

		public SyntaxNode Interpolate()
		{
			return constant
					? InterpolateConstant()
					: InterpolateNonConstant();
		}

		SyntaxNode InterpolateConstant()
		{
			var inlined = InlineEnvironmentVariables(literal);
			return CreateStringLiteral(Unescape(inlined));
		}

		SyntaxNode InterpolateNonConstant()
		{
			var inlined = InlineEnvironmentVariables(literal);

			return CreateStringLiteral(Unescape(inlined));
		}

		static SyntaxNode CreateStringLiteral(string value)
		{
			var text = "@\"" + Verbatimize(value) + "\"";

			return SyntaxFactory.LiteralExpression(
				SyntaxKind.StringLiteralExpression,
				SyntaxFactory.Literal(text, value)
			);
		}

		string LocationDiagnostics(int matchPosition)
		{
			var span = node.GetLocation().GetLineSpan();

			return string.Format(
				"{0} ({1},{2})",
				span.Path,
				span.StartLinePosition.Line + 1,
				span.StartLinePosition.Character + 1 + matchPosition);
		}

		string InlineEnvironmentVariables(string token)
		{
			return environmentVariablePattern.Replace(token, match =>
			{
				var name = match.Groups["variable"].Value;
				var value = Env.Var[name] ?? "?UNDEFINED?";

				Captured.Add(new EnvironmentVariable(name, value));
				return value;
			});
		}

		static string Verbatimize(string token)
		{
			return token.Replace("\"", "\"\"").Replace(@"\\", @"\");
		}

		string Unescape(string token)
		{
			var result = token.Replace("%%", "%");

			return !constant
					? result.Replace("{{", "{").Replace("}}", "}")
					: result;
		}
	}

	struct EnvironmentVariable
	{
		public readonly string Name;
		public readonly string Value;

		public EnvironmentVariable(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public override bool Equals(object obj)
		{
			var other = (EnvironmentVariable)obj;
			return string.Equals(Name, other.Name);
		}

		public override int GetHashCode()
		{
			unchecked { return (Name.GetHashCode() * 397); }
		}
	}
}