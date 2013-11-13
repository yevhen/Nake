using System;
using System.Linq;

using Roslyn.Compilers.CSharp;

namespace Nake.Magic
{
    class ProxyInvocation
    {
        readonly Task task;
        readonly InvocationExpressionSyntax invocation;

        public ProxyInvocation(Task task, InvocationExpressionSyntax invocation)
        {
            this.task = task;
            this.invocation = invocation;
        }

        public ExpressionSyntax Replace()
        {
            var replacement = string.Format(@"Nake.TaskRegistry.Invoke(""{0}"", {1})", 
                                              task.FullName, BuildArgumentString());

            return Syntax.ParseExpression(replacement)
                         .WithLeadingTrivia(invocation.GetLeadingTrivia());
        }

        string BuildArgumentString()
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