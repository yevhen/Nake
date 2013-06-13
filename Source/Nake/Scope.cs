using System;
using System.Linq;

namespace Nake
{
	internal class Scope
	{
		public static Scope Root = new Scope(new string[0]);

		readonly string[] path;

		Scope(string[] path)
		{
			this.path = path;
		}

		public Scope Push(string subscope)
		{
			return new Scope(path.Concat(new[]{subscope}).ToArray());
		}

		public Scope Pop()
		{
			if (path.Length == 0)
				throw new InvalidOperationException();

			return new Scope(path.Take(path.Length - 1).ToArray());
		}

		public string Path()
		{
			return string.Join(":", path);
		}

		public string TaskKey(string taskName)
		{
			return Path() + ":" + taskName;
		}

		public string TaskDisplayName(string taskName)
		{
			return string.Join(":", path.Concat(new[]{taskName}));
		}
	}
}
