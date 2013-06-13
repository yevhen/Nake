using System;

using NUnit.Framework;

namespace Nake.Tests.Testing
{
	public class SubstituteEnvironmentTerminationAttribute : TestActionAttribute
	{
		public override void BeforeTest(TestDetails testDetails)
		{
			Exit.Terminator = (code, msg, ex) =>
			{
				if (ex != null)
					throw new Exception(ex.Message, ex);
			};
		}
	}
}
