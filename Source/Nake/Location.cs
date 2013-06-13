using System;
using System.IO;

namespace Nake
{
	static class Location
	{
		public static Func<string> CurrentDirectory = ()=> Environment.CurrentDirectory;

		public static string GetFullPath(string path)
		{
			return Path.IsPathRooted(path) 
					? path 
					: Path.GetFullPath(Path.Combine(CurrentDirectory(), path));
		}
	}
}
