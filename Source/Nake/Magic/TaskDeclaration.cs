using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    public class TaskDeclaration
    {
        public static bool IsAnnotated(MethodDeclarationSyntax node)
        {
            return node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Task"));
        }

        readonly string path;
        readonly MethodDeclarationSyntax declaration;
        readonly string comments = "";

        public TaskDeclaration(string path, MethodDeclarationSyntax declaration)
        {
            Debug.Assert(path != null);
            Debug.Assert(declaration != null);

            this.path = path;
            this.declaration = declaration;

            var documentation = declaration
                .DescendantNodes(descendIntoTrivia: true)
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (documentation != null)
                comments = documentation.Content.ToString()
                            .Replace("///", "").Trim(' ').TrimEnd('\n').TrimEnd('\r');
        }

        public string FullName
        {
            get
            {
                return DisplayName.Substring(0, DisplayName.IndexOf("(", StringComparison.Ordinal));
            }
        }

        public string DisplayName
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
