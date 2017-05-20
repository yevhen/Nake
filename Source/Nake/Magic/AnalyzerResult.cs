using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
	class AnalyzerResult : SyntaxWalker
	{
		readonly IDictionary<IMethodSymbol, Task> tasksBySymbol = new Dictionary<IMethodSymbol, Task>();
		readonly IDictionary<string, Task> tasksByName = new Dictionary<string, Task>(new CaseInsensitiveEqualityComparer());

		readonly IDictionary<InvocationExpressionSyntax, ProxyInvocation> invocations = new Dictionary<InvocationExpressionSyntax, ProxyInvocation>();
		readonly IDictionary<VariableDeclaratorSyntax, FieldSubstitution> substitutions = new Dictionary<VariableDeclaratorSyntax, FieldSubstitution>();
		readonly IDictionary<LiteralExpressionSyntax, StringInterpolation> interpolations = new Dictionary<LiteralExpressionSyntax, StringInterpolation>();

		public IEnumerable<Task> Tasks
		{
			get { return tasksBySymbol.Values; }
		}

		public Task Find(IMethodSymbol symbol)
		{
			return tasksBySymbol.Find(symbol);
		}

		public void Add(IMethodSymbol symbol, Task task)
		{
			var existent = tasksByName.Find(task.FullName);
			if (existent != null)
				throw DuplicateTaskException.Create(existent, task);

			tasksBySymbol.Add(symbol, task);
			tasksByName.Add(task.FullName, task);
		}

		public void Add(InvocationExpressionSyntax node, ProxyInvocation invocation)
		{
			invocations.Add(node, invocation);
		}

		public ProxyInvocation Find(InvocationExpressionSyntax node)
		{
			return invocations.Find(node);
		}

		public void Add(VariableDeclaratorSyntax node, FieldSubstitution substitution)
		{
			substitutions.Add(node, substitution);
		}

		public FieldSubstitution Find(VariableDeclaratorSyntax node)
		{
			return substitutions.Find(node);
		}

		public void Add(LiteralExpressionSyntax node, StringInterpolation interpolation)
		{
			interpolations.Add(node, interpolation);
		}

		public StringInterpolation Find(LiteralExpressionSyntax node)
		{
			return interpolations.Find(node);
		}
	}
}