using System;
using System.Collections.Generic;
using System.Linq;

namespace Nake;

class Options
{
    public string ScriptFile;
    public string CurrentDirectory;

    public string Framework;
    public string RunnerName;
    public bool DebugScript;
    public bool ResetCache;

    public bool QuietMode;
    public bool SilentMode;
    public bool TraceEnabled;

    public bool ShowHelp;
    public bool ShowVersion;
        
    public bool ShowTasks;
    public string ShowTasksFilter;
        
    public readonly List<Variable> Variables = [];
    public readonly List<Task> Tasks = [];

    static readonly List<Switch> switches =
    [
        new Switch("help", "Display help message and exit")
            .Shortcut("?")
            .OnMatch(options => options.ShowHelp = true),


        new Switch("version", "Display the program version and exit")
            .Shortcut("v")
            .OnMatch(options => options.ShowVersion = true),


        new Switch("quiet", "Do not echo informational messages to standard output")
            .Shortcut("q")
            .OnMatch(options => options.QuietMode = true),


        new Switch("silent", "Same as --quiet but also suppresses user generated log messages")
            .Shortcut("s")
            .OnMatch(options =>
            {
                options.QuietMode = true;
                options.SilentMode = true;
            }),


        new Switch("nakefile FILE", "Use FILE as the Nake project file")
            .Shortcut("f")
            .OnMatch((options, file) => options.ScriptFile = file),


        new Switch("directory DIR", "Use DIR as current directory")
            .Shortcut("d")
            .OnMatch((options, dir) => options.CurrentDirectory = dir),


        new Switch("runner NAME", "Use NAME as runner file name in task listing")
            .OnMatch((options, name) => options.RunnerName = name),


        new Switch("trace", "Enables task execution tracing and full stack traces in exception messages")
            .Shortcut("t")
            .OnMatch(options => options.TraceEnabled = options.DebugScript = true),


        new Switch("debug", "Enables full script debugging in Visual Studio")
            .OnMatch(options => options.DebugScript = true),


        new Switch("framework ID", "Specify .net framework version to use (eg, netcoreapp6.0)")
            .Shortcut("n")
            .OnMatch((options, id) => options.Framework = id),


        new Switch("tasks [PATTERN]", "Display the tasks with descriptions matching optional PATTERN and exit")
            .Shortcut("T")
            .OnMatch((options, filter) =>
            {
                options.ShowTasks = true;
                options.ShowTasksFilter = filter;
            }),


        new Switch("reset-cache", "Resets compilation output cache")
            .Shortcut("r")
            .OnMatch(options => options.ResetCache = true)

    ];

    public static void PrintUsage()
    {
        var banner = $"Usage: {Runner.Label()} [options ...] [VAR=VALUE ...] [task ...]";

        Console.WriteLine(Environment.NewLine + banner);
        Console.WriteLine(Environment.NewLine + "Options:");

        var maxSwitchKeywordLength = switches.Max(x => x.KeywordLength);
        var maxSwitchShortcutsLength = switches.Max(x => x.ShortcutLength);
         
        foreach (var @switch in switches)
        {
            Console.Write("   ");

            @switch.PrintShortcut(maxSwitchShortcutsLength + 2);
            @switch.PrintKeyword(maxSwitchKeywordLength + 2);
            @switch.PrintDescription();

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    public static Options Parse(string[] args)
    {
        var result = new Options();
            
        var remaining = StripArgSeparator(args);
        if (remaining.Length == 0)
            return result;
            
        remaining = ParseSwitches(remaining, result);
        if (remaining.Length == 0)
            return result;

        remaining = ParseVariables(remaining, result);
        if (remaining.Length == 0)
            return result;

        if (remaining.Length != 0)
            ParseTasks(remaining, result);

        return result;
    }

    static string[] StripArgSeparator(string[] args) => 
        args.Length > 1 && args[0] == "--"
            ? args.Skip(1).ToArray()
            : args;


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

        return Slice(args, position);
    }

    static string[] ParseVariables(string[] args, Options options)
    {
        foreach (var arg in args.TakeWhile(Variable.Matches))
        {
            options.Variables.Add(new Variable(arg));
        }

        return Slice(args, options.Variables.Count);
    }

    static void ParseTasks(string[] args, Options options)
    {            
        var match = new Task.Match(args);

        while (match != null)
        {
            options.Tasks.Add(match.Build());

            match = match.Next();
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

        string shortcut = "";
        Action<Options, string> handler;

        public Switch(string pattern, string description)
        {
            this.pattern = pattern;
            this.description = description;

            var specification = pattern.Split([' '], StringSplitOptions.RemoveEmptyEntries);
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

        public int ShortcutLength
        {
            get { return shortcut.Length + ShortcutIndicator.Length; }
        }

        public void PrintDescription()
        {
            Console.Write(description);
        }

        public void PrintKeyword(int padding)
        {
            Print(KeywordIndicator + pattern, padding);
        }

        public void PrintShortcut(int padding)
        {
            Print(shortcut != "" ? ShortcutIndicator + shortcut : "", padding);
        }

        static void Print(string s, int padding)
        {
            Console.Write(s.PadRight(padding));
        }

        public Switch Shortcut(string alias)
        {
            shortcut = alias;
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
            if (arg.StartsWith(KeywordIndicator))
                return keyword == arg.Remove(0, KeywordIndicator.Length);

            if (arg.StartsWith(ShortcutIndicator))
                return shortcut.Contains(arg.Remove(0, ShortcutIndicator.Length));
            
            return false;
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

    static T[] Slice<T>(T[] source, int start)
    {
        var length = source.Length - start;

        if (length == 0)
            return [];

        var slice = new T[length];
        Array.Copy(source, start, slice, 0, length);

        return slice;
    }

    public class Variable
    {
        public static bool Matches(string arg)
        {
            return arg.Contains("=");
        }

        public readonly string Name;
        public readonly string Value;

        public Variable(string arg)
        {
            var parts = arg.Split('=');

            Name = parts[0];
            Value = parts[1];
        }
    }

    public class Task
    {
        public static Task Default = new("default", []);

        public readonly string Name;
        public readonly TaskArgument[] Arguments;

        public Task(string name, TaskArgument[] arguments)
        {
            Name = name;
            Arguments = arguments;

            Check();
        }

        void Check()
        {
            var isNamedStarted = false;

            foreach (var arg in Arguments)
            {
                if (isNamedStarted && arg.IsPositional())
                    throw new TaskArgumentOrderException(Name);

                isNamedStarted = arg.IsNamed();
            }
        }

        public class Match
        {
            readonly string name;
            readonly string[] remaining;
            readonly IList<Argument> arguments = new List<Argument>();

            public Match(string[] args)
            {
                name = args[0];
                
                var current = new Argument();
                arguments.Add(current);
                    
                var position = 0;
                while (++position < args.Length)
                {
                    var arg = args[position];
                    
                    if (arg == ";")
                    {
                        current.Terminate();
                        position++;
                        break;
                    }

                    if (current.IsComplete())
                    {
                        current.Terminate();
                        arguments.Add(current = new Argument());
                    }

                    if (arg.Contains("="))
                    {
                        var parts = arg.Split('=');
                            
                        if (parts.Length > 1)
                        {
                            current.SetName(parts[0]);
                            current.SetValue(string.Join("=", Slice(parts, 1)));
                            continue;
                        }
                    }

                    if (arg.StartsWith("--"))
                    {
                        current.SetName(arg.Substring(2).Replace("-", ""));
                        current.SetValue("true");
                        continue;
                    }

                    current.SetValue(arg);
                }
                    
                current.Terminate();
                remaining = Slice(args, position);
            }

            public Task Build()
            {
                return new Task(name, arguments.Where(x => !x.IsEmpty())
                    .Select(x => x.Build())
                    .ToArray());
            }

            public Match Next()
            {
                return remaining.Length != 0 ? new Match(remaining) : null;
            }

            class Argument
            {
                string name;
                string value;

                public void SetValue(string arg)
                {
                    value = arg;
                }

                public void SetName(string arg)
                {
                    name = arg;
                }

                public void Terminate()
                {
                    if (!IsEmpty() && !IsComplete())
                        throw OptionParseException.IncompleteArgument(name);
                }

                public bool IsEmpty()
                {
                    return name == null && value == null;
                }

                public bool IsComplete()
                {
                    return (name == null && value != null) || (name != null && value != null);
                }

                public TaskArgument Build()
                {
                    return new TaskArgument(name ?? "", value);
                }
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

    public static Exception IncompleteArgument(string name)
    {
        return new OptionParseException("Incomplete argument {{{0}}}", name);
    }
}