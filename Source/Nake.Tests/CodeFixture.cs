using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Nake.Tests
{
	internal abstract class CodeFixture
	{
		[SetUp]
		public void SetUp()
		{
			TaskRegistry.Global = new TaskRegistry();
		}

		protected static void Invoke(string taskName, params TaskArgument[] args)
		{
			TaskRegistry.Invoke(taskName, args);
		}

		protected static void Build(string code, Dictionary<string, string> substitutions = null)
		{
			var engine = new Engine();

			var result = engine.Build(
				code, substitutions ?? new Dictionary<string, string>(), false
			);

			TaskRegistry.Global = new TaskRegistry(result);
		}

		protected static IEnumerable<Task> Tasks
		{
			get { return TaskRegistry.Global.Tasks; }
		}

		protected static Task Find(string taskName)
		{
			return TaskRegistry.Global.Find(taskName);
		}
	}
}