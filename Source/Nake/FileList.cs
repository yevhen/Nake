using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using GlobDir;

namespace Nake
{
	public class FileList : IEnumerable<string>
	{
		readonly HashSet<string> includes = new HashSet<string>();
		readonly List<Func<string, bool>> excludes = new List<Func<string, bool>>();

		public FileList()
		{}

		public FileList(params string[] patterns)
		{
			foreach (var pattern in patterns)
			{
				Include(pattern);
			}
		}

		public FileList Include(string pattern)
		{
			foreach (var inclusion in pattern.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries))
			{
				includes.Add(inclusion.Replace(@"\", "/"));
			}

			return this;
		}

		public FileList Exclude(string pattern)
		{
			Exclude(
				new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", 
				RegexOptions.IgnoreCase | RegexOptions.Singleline));

			return this;
		}

		public FileList Exclude(Regex regex)
		{
			Exclude(regex.IsMatch);

			return this;
		}

		public FileList Exclude(Func<string, bool> predicate)
		{
			excludes.Add(predicate);

			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<string> GetEnumerator()
		{
			var entries = new HashSet<string>(

				includes				
					.SelectMany(pattern => 
						Glob.GetMatches(AbsoluteGlob(pattern))
							.Where(file => !excludes.Any(predicate => predicate(file.Replace("/", @"\")))))
								.Select(file => file.Replace("/", @"\"))
			);

			return entries.GetEnumerator();
		}

		static string AbsoluteGlob(string pattern)
		{
			if (Path.IsPathRooted(pattern))
				return pattern;

			var absolutePath = Path.Combine(Location.CurrentDirectory(), pattern).Replace("\\", "/");

			return absolutePath;
		}

		public static implicit operator FileList(string[] patterns)
		{
			return new FileList(patterns);
		}

		public static implicit operator FileList(string pattern)
		{
			return new FileList(pattern);
		}

		public static implicit operator string[](FileList files)
		{
			return files.ToArray();
		}
	}
}