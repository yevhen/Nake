using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
	public class TaskDeclaration
	{
		readonly string path;
		readonly MethodDeclarationSyntax declaration;
		readonly bool step;
		readonly string comments = "";

		public TaskDeclaration(string path, MethodDeclarationSyntax declaration, bool step)
		{
			Debug.Assert(path != null);
			Debug.Assert(declaration != null);

			this.path = path;
			this.declaration = declaration;
			this.step = step;

			var documentation = declaration
				.DescendantNodes(descendIntoTrivia: true)
				.OfType<DocumentationCommentTriviaSyntax>()
				.FirstOrDefault();

			if (documentation != null)
				comments = documentation.Content.ToString()
							.Replace("///", "").Trim(' ').TrimEnd('\n').TrimEnd('\r');
		}

		public bool IsStep
		{
			get { return step; }
		}

		public string DisplayName
		{
			get { return FullName.ToLowerInvariant(); }
		}

		public string FullName
		{
			get
			{
				return Signature.Substring(0, Signature.IndexOf("(", StringComparison.Ordinal));
			}
		}

		public string Signature
		{
			get
			{
				var signature = declaration.Identifier.ToString();
				signature += declaration.ParameterList.ToString();
				return path == "" ? signature : path + "." + signature;
			}
		}

		public string Summary
		{
			get { return comments; }
		}
	}
}