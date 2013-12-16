## Nake

Nake is a magic task runner tool for .NET. It was built by unicorns flying in a rainbows and it's well seasoned by lots of fairy dust. Nake is simply that build/deployment/ops automation tool you were dreaming of.

Jokes aside, it's the only tool which can give you an imperative/functional convenience of Rake/MSBuild, but without forcing you (and your team) to learn yet another language and without pricking you eyes by hordes of angle brackets. 

At last! Now you can automate your tasks by writing a 100% idiomatic C# code without any limitations (and with all the power of .NET framework) using a lightweight scripting approach. No projects, no pre-compilation, using any text editor. 

Nake's DSL for defining tasks is uniquely minimal and it was carefully crafted to be a 100% IntelliSense compatible. And no worries, Nake has great MSBuild interoperability story, so you can easily import and execute any built-in or third-party MSBuild tasks.

## Getting Started

### How to install

There multiple ways in which Nake could be installed. You can install it by using NuGet [package](https://www.nuget.org/packages/Nake), or you can install it by downloading a [standalone](https://github.com/yevhen/Nake/releases) executable from GitHub releases page, and of course you can always build it from sources. 

To install Nake via NuGet, run this command in NuGet package manager console:

	PM> Install-Package Nake 

#### IntelliSense and syntax highlighting

You can install [Roslyn CTP](http://www.microsoft.com/en-us/download/details.aspx?id=34685) extension to get IntelliSense support in Visual Studio 2012. IntelliSense for other versions of Visual Studio is not available at the moment. 

Syntax highlighting for `.csx` files is easy to get - it is just standard C# code, so just map `.csx` file extension in you favorite text editor to be recognized as C#.

### Writing your first task

Open up you favorite text editor and enter the following code:

```cs
[Task] public static void Welcome()
{
	Console.WriteLine("Hello, world!");
}
```
Save it to the root folder of your solution and give file `Nake.csx` name. Now you can invoke your task from command line by using Nake console application.

> NOTE: If you installed Nake via NuGet package manager console in Visual Studio, during an installation a sample script (`Nake.csx`) was created in the root directory of your solution. You can use it as the starting point.

### Invoking task via command line

To invoke a task from a command line you just need to pass it's name to the Nake's console application. Task names are case insensitive, so to invoke the task you've just created, simply execute the following in your favorite console:

	Nake welcome

You should see the following output:

	Hello, world!

Cool, ya? :grimacing: 

That for sure will only work if you have `Nake.exe` somewhere in your path. That's not good as Nake should be used as local dependency. Assuming that you have installed it via NuGet, your actual path might look like the one below:

	Packages\Nake.1.0.0.7\tools\net45\Nake.exe welcome

That, without doubt, is tedious to enter every time you want to invoke a task, but there is nothing in the world that cannot be fixed with a duct tape and bit of cleverness. Let's create a simple batch file which will act as the proxy for running Nake. Create `Nake.bat` file in the root directory of your solution and put there the text below:

	@ECHO OFF 
	Packages\Nake.1.0.0.7\tools\net45\Nake.exe %*

Now you have an easy (and recommended) way to launch Nake.

> NOTE: If you installed Nake via NuGet package manager console in Visual Studio, during an installation a sample batch runner file (`Nake.bat`) was created in the root directory of your solution. if that didn't happen - check Nake `samples` directory inside respective package folder. 

### Describing tasks

Now if you invoke Nake with `-T` switch it should show you all tasks defined in the script. Nevertheless, running it for the script which we've just created will produce no output. Why is that? Well, that's because by default Nake won't list tasks that don't have descriptions.

Now guess how can we give a meaningful descriptions to our tasks, which are simply methods? Right, you will just use standard XML documentation comments:  

```cs
/// <summary>
/// This is a demo task
/// </summary>
[Task] public static void Welcome()
{
	Console.WriteLine("Hello, world!");
}
```

Now if you run Nake with `-T` switch you should see the following output: 

	C:\Demo>Nake -T
	
	Nake welcome  # This is a demo task


### Passing arguments to a task

With Nake, it's ridiculously easy to define task parameters and pass them to a task from a command line. 

```cs

[Task] public static void Welcome(string who)
{
	Console.WriteLine("Hello, {0}!", who);
}
```

Now you can simply invoke it using the following command-line:

	Nake welcome amigo

That will print as expected:

	Hello, amigo!

Nake also supports optional parameters, so you can define the task above as follows:

```cs
[Task] public static void Welcome(string who = "baby", string greeting = "Hello")
{
	Console.WriteLine("{0}, {1}!", greeting, who);
}
```

Invoking it with the following command line:

	Nake welcome greeting: "Hasta la vista"

Will produce the following output:

	Hasta la vista, baby!

> NOTE: At the moment Nake supports the following parameter types: bool, string and int. Support for `params` arrays and some other types is on a road-map.  Nevertheless, it's possible to code pretty much anything with just the types already supported by Nake.

### Specifying prerequisite tasks (dependencies)

Suppose we have the following build related tasks defined in a script:

```cs
[Task] public static void Clean()
{
	// here goes code, which cleans build output directory
}

[Task] public static void Build(string configuration = "Debug")
{
	// here goes code, which builds sources using given configuration
}
```

We want to specify that the `Clean()` task should always be executed before the `Build()` task is executed. Regarding to this, we can say that the `Clean()` task is the *prerequisite* of the `Build()` task or that the `Build()` task is *dependent* on the `Clean()` task. 

Now, how can we specify that relationship in Nake script? That `Build()` task is dependent on the `Clean()` task? Check out the following example:

```cs
[Task] public static void Clean()
{
	// here goes code, which cleans build output directory
}

[Task] public static void Build(string configuration = "Debug")
{
	Clean();

	// here goes code, which builds sources using given configuration
}
```
Yes, it's just a regular method call. There is no any special syntax in Nake for specifying prerequisites - you simply invoke one task from within another task using native C# language constructs. Everything is 100% statically bound. No strings whatsoever, and you can put task invocations in any place you want (so that you can easily replicate advanced MSBuild features like [Before/After targets](http://freetodev.wordpress.com/2009/06/24/msbuild-4-0-beforetargets-and-aftertargets/) :grin:). 

##### How does it work?

DOC: Describe how Nake is unique in its approach to specifying tasks and their prerequisites, and how it extends C# language semantics in order to implement dependency based style of computation ([link](http://martinfowler.com/articles/rake.html#DependencyBasedProgramming)), while still keeping its DSL (pure C#) rather imperative. Describe how Nake uses Roslyn's syntax re-writing features to rewrite task invocations.

### Namespaces
DOC

## Other mind-blowing features (at a glance)

#### String literal expansions
DOC

#### Command line overrides
DOC 

#### MSBuild interoperability
DOC

#### Powerful utility library
DOC

## Other useful features

- **Cycle dependencies detection** - DOC
- **Roslyn bootstrapping** - DOC

## General scripting

DOC: Point to relevant topics in scriptcs documentation

## Working with command line

General syntax is: `Nake [options ...]  [VAR=VALUE ...]  [task ...]`

Options:

	   -?  --help             Display help message and exit
	   -v  --version          Display the program version and exit
	   -q  --quiet            Do not echo informational messages to standard output
	   -s  --silent           Same as --quiet but also suppresses user generated log messages
	   -f  --nakefile FILE    Use FILE as the Nake project file
	   -d  --directory DIR    Use DIR as current directory
	   -t  --trace            Enables task execution tracing and full stack traces in exception messages
	       --debug            Enables full script debugging in Visual Studio
	   -T  --tasks [PATTERN]  Display the tasks with descriptions matching optional PATTERN and exit

>NOTE: You can always get help for command line usage by running Nake with `-?` or `--help` switches.

#### Setting environment variables

You can set process-level environment variables by defining key/value pairs before task name:

```
Nake CpuCount=2 OutDir=C:\Temp scan 
```

#### Overriding constants

An interesting feature of Nake, is that it allows you to override value of any public constant or public static property, by simply passing a corresponding key/value pair from a command line, like in previous example:

```
Nake CpuCount=2 OutDir=C:\Temp scan 
```
You can get these values using built-in utility class:

```cs
var cpuCount = Env.Var["CpuCount"] ?? 1;
var outDir = Env.Var["OutDir"] ?? "Output";
```
This is similar to MSBuild way of having overridable variables with fallback values. But in Nake, it is enough to just declare public constants or public static properties, and Nake will try to match property names with those coming from a command line, overriding those which match: 

```cs
public const int CpuCount = 1;
public static string OutDir = "Output";
```
Declaring constants could be more handy, as you can use them as default parameter values: 

```cs
[Task] public static void Clean(string dir = OutDir) { ... }
```

#### Passing arguments to tasks 

Use space to separate arguments. Both positional and named arguments are supported. The command line syntax is the same as regular C# method calling syntax, except space is used as separator (instead of `,`):

```
Nake db.migrate MainDb version:10002 
```
> NOTE: Use double quotes, if the value you're passing contains colon `:` (ie `Nake clean dir:"C:\Temp"`). Also, don't forget to properly escape symbols, which have special meaning in your shell.  

#### Calling multiple tasks 

You can call multiple tasks, within one session, by simply separating them with semicolon `;`:

```
Nake clean;build;test 
```
> NOTE: In some shells, like PowerShell, semicolon (`;`) need to be escaped. For PowerShell it should be escaped with a backtick `;.

## Backlog

- Running on Mono
- Support for additional parameter types, such as `params`
- Ask for required arguments, ala PowerShell
- Interactive mode, task name tab completion
- PowerShell hosting
- On-demand script loader
- Once Roslyn finally start respecting #load directive, move both Meta and Utility to `.csx` files, so global install is possible

## Contributing

**Bugs** - no need to ask anything, just fix it and do a pull request

**Features** - ideally should be discussed via GitHub issues or Nake's discussion group to avoid duplicate work and to make sure that new stuff is still inline with original vision.

## Samples and Documentation

Have a look at [Nake.csx](https://github.com/yevhen/Nake/blob/master/Nake.csx) or [Publish.csx](https://github.com/yevhen/Nake/blob/master/Publish.csx). Those are the Nake files used to build and publish Nake itself (ye, we're eating our own dog food). Also, additional samples can be contributed to samples [repository](https://github.com/yevhen/Nake-samples). Detailed documentation (soon) can be found on [wiki](https://github.com/yevhen/Nake/wiki). 

## Community

General discussion group could be found [here](https://groups.google.com/forum/#!forum/naketool). Also, for news you can follow Nake's [official](https://twitter.com/NakeTool) twitter account (or [my](https://twitter.com/yevhen) account for that matter). The twitter's hashtag is `#naketool`.

## Credits
- To all contributors (check out GitHub statistics)!
- Thanks to all members of the [scriptcs](https://github.com/scriptcs) team for lending me their script pre-processing code
- Special thanks to [Anton Martynenko](https://twitter.com/aamartynenko) for giving me an idea and steering Nake's DSL in the right direction
- Hugs and kisses to my lovely Valery for being so patient and supportive, and for feeding me food and beer, while I was sitting in my cage working on Nake, instead of spending that time with her

## License

Apache 2 License