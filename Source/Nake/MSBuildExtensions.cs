using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nake
{
	public static class MSBuildExtensions
	{
		public static ITaskItem[] AsTaskItems(this IEnumerable<string> sequence)
		{
			return sequence.Select(AsTaskItem).ToArray();
		}

		public static ITaskItem AsTaskItem(this string s)
		{
			return new TaskItem(s);
		}
	}
}
