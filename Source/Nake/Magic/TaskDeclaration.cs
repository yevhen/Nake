using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    public class TaskDeclaration : IEquatable<TaskDeclaration>
    {
        public TaskDeclaration(string path, MethodDeclarationSyntax declaration, bool step)
        {
            Debug.Assert(path != null);
            Debug.Assert(declaration != null);

            IsStep = step;

            var documentation = declaration
                .DescendantNodes(descendIntoTrivia: true)
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (documentation != null)
                Summary = documentation.Content.ToString()
                    .Replace("///", "").Trim(' ').TrimEnd('\n').TrimEnd('\r');

            Signature = TaskSignature(declaration, path);
            FullName = Signature.Substring(0, Signature.IndexOf("(", StringComparison.Ordinal));
        }

        public string Signature { get; }
        public string FullName { get; }
        public string Summary { get; } = "";
        public bool IsStep { get; }

        public string DisplayName => FullName.ToLowerInvariant();

        static string TaskSignature(MethodDeclarationSyntax method, string path)
        {
            var signature = method.Identifier.ToString();
            signature += method.ParameterList.ToString();
            return path == "" ? signature : path + "." + signature;
        }

        public bool Equals(TaskDeclaration other) => 
            !ReferenceEquals(null, other) && 
            (ReferenceEquals(this, other) || 
            FullName == other.FullName);

        public override bool Equals(object obj) => 
            !ReferenceEquals(null, obj) && 
            (ReferenceEquals(this, obj) || 
            obj.GetType() == GetType() && Equals((TaskDeclaration) obj));

        public override int GetHashCode() => (FullName != null ? FullName.GetHashCode() : 0);

        public static bool operator ==(TaskDeclaration left, TaskDeclaration right) => Equals(left, right);
        public static bool operator !=(TaskDeclaration left, TaskDeclaration right) => !Equals(left, right);
    }
}
