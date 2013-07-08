using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using GlobDir;

namespace Nake
{
	public class FileList : IEnumerable<string>
	{
		readonly List<string> includes = new List<string>();
		readonly List<Func<string, bool>> excludes = new List<Func<string, bool>>();

		public FileList()
		{}

		public FileList(params string[] includes)
		{
			foreach (var pattern in includes)
			{
				Include(pattern);
			}
		}

		public FileList Include(string pattern)
		{
			var include = pattern.Replace(@"\", "/");

			if (!includes.Contains(include))
				includes.Add(include);

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
							.Where(file => !excludes.Any(predicate => predicate(file))))
								.Select(file => file.Replace("/", @"\"))
			);

			return entries.GetEnumerator();
		}

		static string AbsoluteGlob(string pattern)
		{
			return string.Format("{0}" + pattern, Location.CurrentDirectory().Replace("\\", "/") + "/");
		}
	}
}