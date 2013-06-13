using System;
using System.Collections.Generic;
using System.Linq;

namespace Nake
{
	class InvocationChain
	{
		public static InvocationChain Start = new InvocationChain(new List<Task>());

		readonly List<Task> chain;
		
		InvocationChain(List<Task> chain)
		{
			this.chain = chain;
		}

		public InvocationChain Append(Task invocation)
		{
			if (chain.Contains(invocation))
				throw new CyclicDependencyException(Tail(), invocation, Via(invocation));

			return new InvocationChain(new List<Task>(chain) {invocation});
		}

		Task Tail()
		{
			return chain[chain.Count - 1];
		}

		string Via(Task invocation)
		{
			return string.Join(" => ", 
				chain
				.SkipWhile(x => x != invocation)
				.Concat(new[]{invocation})
				.Select(x => x.DisplayName)
				.ToArray()
			);
		}
	}
}
