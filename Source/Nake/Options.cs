using System;
using System.Collections.Generic;
using System.Linq;

namespace Nake
{
	class Options
	{
		public string ProjectFile;
		public string CurrentDirectory;
		
		public bool QuietMode;
		public bool SilentMode;
		public bool TraceEnabled;

		public bool ShowHelp;
		public bool ShowVersion;
		
		public bool ShowTasks;
		public string ShowTasksFilter;

		public readonly IDictionary<string, string> Variables = new Dictionary<string, string>();
		public readonly HashSet<string> Tasks = new HashSet<string>();

		static readonly List<Switch> switches = new List<Switch>
		{
			new Switch("help", "Display help message and exit")
				.Shortcuts("?")
				.OnMatch(options => options.ShowHelp = true),

			new Switch("version", "Display the program version and exit")
				.Shortcuts("v")
				.OnMatch(options => options.ShowVersion = true),			
				
			new Switch("quiet", "Do not echo informational messages to standard output")
				.Shortcuts("q")
				.OnMatch(options => options.QuietMode = true),

			new Switch("silent", "Same as --quiet but also suppresses user generated log messages")
				.Shortcuts("s")
				.OnMatch(options =>
				{
					options.QuietMode = true; 
					options.SilentMode = true;
				}),

			new Switch("nakefile FILE", "Use FILE as the Nake project file")
				.Shortcuts("f")
				.OnMatch((options, file) => options.ProjectFile = file),

			new Switch("directory DIR", "Use DIR as current directory")
				.Shortcuts("d")
				.OnMatch((options, dir) => options.CurrentDirectory = dir),

			new Switch("trace", "Enables task execution tracing and full stack traces in exception messages")
				.Shortcuts("t")
				.OnMatch(options => options.TraceEnabled = true),
			
			new Switch("tasks [PATTERN]", "Display the tasks with descriptions matching optional PATTERN and exit")
				.Shortcuts("T")
				.OnMatch((options, filter) => 
				{ 
					options.ShowTasks = true;
					options.ShowTasksFilter = filter;
				}),										
		};

		public static void PrintUsage()
		{
			const string banner = "Usage: nake [options ...]  [VAR=VALUE ...]  [targets ...]";

			Console.WriteLine(Environment.NewLine + banner);
			Console.WriteLine(Environment.NewLine + "Options:");

			var maxSwitchKeywordLength = switches.Max(x => x.KeywordLength);
			var maxSwitchShortcutsLength = switches.Max(x => x.ShortcutsLength);

			foreach (var @switch in switches)
			{
				Console.Write("   ");

				@switch.PrintShortcuts(maxSwitchShortcutsLength + 2);
				@switch.PrintKeyword(maxSwitchKeywordLength + 2);
				@switch.PrintDescription();

				Console.WriteLine();
			}

			Console.WriteLine();
		}
			
		public static Options Parse(string[] args)
		{
			var result = new Options();

			var remaining = ParseSwitches(args, result);
			if (remaining.Length == 0)
				return result;

			remaining = ParseVariables(remaining, result);
			if (remaining.Length == 0)
				return result;

			ParseTasks(remaining, result);
			return result;
		}

		static string[] ParseSwitches(string[] args, Options options)
		{
			var position = 0;

			while (position < args.Length)
			{
				Switch.Match match = null;

				foreach (var option in switches)
				{
					match = option.TryMatch(args, position);
					
					if (match == null)
						continue;

					match.Apply(options);
					position += match.ArgumentsConsumed();

					break;
				}

				if (match == null)
					break;
			}

			return args.Slice(position);
		}

		static string[] ParseVariables(string[] args, Options options)
		{
			var position = 0;

			while (position < args.Length)
			{
				if (!args[position].Contains("="))
					break;

				var keyValue = args[position].Split('=');
				options.Variables.Add(keyValue[0], keyValue[1]);

				position++;
			}

			return args.Slice(position);
		}

		static void ParseTasks(IEnumerable<string> args, Options options)
		{
			foreach (var arg in args)
			{
				options.Tasks.Add(arg);
			}
		}

		class Switch
		{
			const string KeywordIndicator = "--";
			const string ShortcutIndicator = "-";

			readonly string keyword;
			readonly string pattern;
			readonly string description;
			
			readonly bool expectsValue;
			readonly bool requiresValue;

			string[] shortcuts = new string[0];
			Action<Options, string> handler;

			public Switch(string pattern, string description)
			{
				this.pattern = pattern;
				this.description = description;

				var specification = pattern.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				keyword = specification[0];

				var hasValueSpecified = specification.Length == 2;
				if (!hasValueSpecified)
					return;
				
				expectsValue = true;
				requiresValue = !specification[1].Contains("[");
			}

			public int KeywordLength
			{
				get { return pattern.Length + KeywordIndicator.Length; }
			}

			public int ShortcutsLength
			{
				get { return string.Join("/", shortcuts).Length + ShortcutIndicator.Length; }
			}

			public void PrintKeyword(int padding)
			{
				Console.Write((KeywordIndicator + pattern).PadRight(padding));
			}

			public void PrintShortcuts(int padding)
			{
				Console.Write((ShortcutIndicator + string.Join("/", shortcuts)).PadRight(padding));
			}

			public void PrintDescription()
			{
				Console.Write(description);
			}

			public Switch Shortcuts(params string[] aliases)
			{
				shortcuts = aliases;
				return this;
			}

			public Switch OnMatch(Action<Options> action)
			{
				handler = (options, value) => action(options);
				return this;
			}

			public Switch OnMatch(Action<Options, string> action)
			{
				handler = action;
				return this;
			}

			public Match TryMatch(string[] args, int position)
			{
				return Matches(args[position])
						? new Match(this, GetValue(args, position))
						: null;
			}

			bool Matches(string arg)
			{
				var matchKeyword = keyword == arg.Remove(0, KeywordIndicator.Length);
				var matchAnyShortcut = shortcuts.Contains(arg.Remove(0, ShortcutIndicator.Length));

				return matchKeyword || matchAnyShortcut;
			}

			string GetValue(string[] args, int position)
			{
				if (!expectsValue)
					return null;

				var hasValue = position + 1 != args.Length;

				if (requiresValue && !hasValue)
					throw OptionParseException.MissingValue(keyword);

				return hasValue ? args[position + 1] : null;
			}

			public class Match
			{
				readonly Switch option;
				readonly string value;

				public Match(Switch option, string value)
				{
					this.option = option;
					this.value = value;
				}

				public void Apply(Options options)
				{
					option.handler(options, value);
				}

				public int ArgumentsConsumed()
				{
					return value != null ? 2 : 1;
				}
			}
		}
	}

	class OptionParseException : Exception
	{
		OptionParseException(string message, params object[] args)
			: base(string.Format(message, args))
		{}

		public static OptionParseException MissingValue(string keyword)
		{
			return new OptionParseException("Switch -{0} is missing required value");
		}
	}
}
