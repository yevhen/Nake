using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    class ProxyInvocation
    {
        readonly Task task;

        public ProxyInvocation(Task task)
        {
            this.task = task;
        }

        public ExpressionSyntax Replace(InvocationExpressionSyntax invocation)
        {
            var replacement = string.Format(@"Nake.TaskRegistry.Invoke(""{0}"", {1})",
                                              task.FullName, BuildArgumentString(invocation));

            return SyntaxFactory.ParseExpression(replacement)
                         .WithLeadingTrivia(invocation.GetLeadingTrivia());
        }

        static string BuildArgumentString(InvocationExpressionSyntax invocation)
        {
            var arguments = invocation.ArgumentList.Arguments;

            return arguments.Count != 0
                ? string.Join(", ", arguments.Select(FormatArgument))
                : "new Nake.TaskArgument[0]";
        }

        static string FormatArgument(ArgumentSyntax argument)
        {
            return string.Format(@"new Nake.TaskArgument(""{0}"", {1})",
                                 GetArgumentName(argument), GetArgumentValue(argument));
        }

        static string GetArgumentName(ArgumentSyntax argument)
        {
            return argument.NameColon != null
                        ? argument.NameColon.Name.Identifier.ValueText
                        : "";
        }

        static SyntaxNode GetArgumentValue(ArgumentSyntax argument)
        {
            return argument.Expression;
        }
    }
}