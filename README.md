![Nake](Logo.Wide.png)

Nake is a magic task runner tool for .NET Core. It's a hybrid of Shovel and Rake. The DSL for defining tasks is uniquely minimal and it's just plain (_naked_) C# code! Nake is built on top of the latest Roslyn release so you can use all of the latest C# features in you scripts and even more.

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/yevhen/Nake?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/kors8n8y4r4xklop/branch/master?svg=true)](https://ci.appveyor.com/project/yevhen/nake/branch/master)
[![NuGet](https://img.shields.io/nuget/v/Nake.svg?style=flat)](https://www.nuget.org/packages/Nake/)

## Terms of use

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

To learn more about the war and how you can help, [click here](https://github.com/yevhen/yevhen). Glory to Ukraine! 🇺🇦

### How to install

Nake is built as dotnet tool. You can install it either globally of [locally](https://stu.dev/dotnet-core-3-local-tools/) (recommended):

	PM> dotnet tool install Nake

## Syntax cheatsheet

```cs
#r "System                          //
#r "System.Core"                    //
#r "System.Data"                    //   #reference assemblies from the netstandard BCL   
#r "System.Xml"                     //       (these are referenced by default)
#r "System.Xml.Linq"                //    
#r "Microsoft.CSharp"               //

#r "System.IO, Version=4.0..."      //  you can reference assembly by its full name
#r "./Packages/nunit.dll"           //        or by using relative path
#r "/usr/bin/SDK/Orleans.dll"       //            or by absolute path
                                        
#r "nuget: Streamstone, 2.0.0"      //  Nake is using dotnetscript dependency resolution
#r "nuget: Newtonsoft.Json"         //  so referencing nuget packages is fully supported
#r "nuget: Orleankka, 2.*"          //  https://github.com/filipw/dotnet-script#nuget-packages-1

#load "Other.csx"                   //      #load code from other script files
#load "Build/Another.csx"           //  (both absolute and relative paths are fine)

using System;                       //
using System.IO;                    //      standard C# namespace imports
using System.Linq;                  //     (these are imported by default)
using System.Text;                  //  
using System.Threading.Tasks;       //    
using System.Collections.Generic;   //  

using static System.IO.Path;        //    C# "using static members" feature 
using static System.Console;        //      will make you scripts more terse

WriteLine("Are you ready? Y/N:");   //      any code you put on the script level 
if (ReadLine() == "N")              //  will run before any of the tasks are executed
    Exit("See you soon ...");       //      (useful for one-off initialization)

var greeting = "Hello";             //   you can override any script-level variables 
var who = "world";                  //  with the values passed from the command line

/// Prints greeting                 //  this F#-style summary is shown in task listing
[Nake] void Welcome()               //  [Nake] makes method runnable from command line
{                                       
    Write($"{greeting},{who}!");    //  forget ugly string.Format & string concatenation 
}                                   //   with built-in support for string interpolation

[Nake] void Tell(
    string what = "Hello",          //     for parameterized tasks you can supply
    string whom = "world",          //     arguments directly from the command line
    int times = 1,                  //          (string, int, boolean and 
    DayOfWeek when,                 //         enum arguments are supported)
    bool quiet = false              //  + switch-like syntax for booleans (eg, --quiet)
)
{
    var emphasis = quiet ? "" : "!";
    for (; times > 0; times--)
	    WriteLine($"{what}, {whom} on {when}{emphasis}");
}                                   

[Step] void Clean()                  //     Steps are tasks with 'run once' semantics      
{                                    //     (foundation of any build automation tool)
    Delete($"{OutputPath}/*.*");	
}                                   

[Step] void Build(string cfg = "Debug")
{					                    
    Clean();                        //  unlike popular tools, there is no special syntax
    -------                         //     for specifying task (step) dependencies
    MSBuild("Nake.sln", cfg);       //    (it's just plain old C# method invocation)
}                                       
                                       
[Step] void Test()
{					                    
    Clean();                        //     you have complete control over decision,
    Build();                        //  when and in what order dependent steps should run
    -------                         //      (and Nake makes sure of run-once behavior)
    NUnit($"{OutputPath}/*.Tests.dll");   
}

[Step] void Publish(bool beta = false)
{					                    
    Test();                         //   sometimes, you need to execute the same step but with
    Build("Release");               //  different args. Unlike other build automation tools
    ------                          //  there is no special syntax to force step to run again, 
    Nuget("Nake.nuspec", beta);     //       you just invoke it with different arguments!
}                                       

var apiKey = "$NugetKey$";          //      $var$ is the shortcut syntax for getting 
Push(apiKey, "{PackagePath}");      //          value of environment variable

const string ApiKey = "$APIKEY$";   //     environment variables defined in constant scopes
void Push(string key = "$APIKEY$")  //       evaluated once at compile-time (ie, inlined)

var ApiKey = "$APIKEY$";            //     environment variables defined in variable scopes
Write("$APIKEY$");                  //       evaluated at invocation-time (dynamic)

Write("$NakeStartupDirectory$");    //       these special environment variables
Write("$NakeWorkingDirectory$");    //        are automatically created by Nake

var root = "$NakeScriptDirectory$"; //   this is how you can get script directory inlined
Write(Location.NakeScriptDirectory) //   alternatively call this method from Nake.Utility

Write("{{esc}}");                   //  will simply print {esc} (no string interpolation)
Write("$$esc$$");                   //  will simply print $esc$ (no env variable inlining)

Cmd($"docker build -f {spec} .");   //        you can use Shell.Cmd to execute 
Cmd($"echo {title}");               //         commands via shell interpreter ...

r = await Run($"docker images");    //      and you can get the result of the execution
Write(result.StandardOutput);       //    by using functionality provided by MedallionShell

Run("app") < new FileInfo("input")  //   this includes fancy composable piping and redirects

await $"docker rm {cid} .";         //      or simply await the cli string to get it executed
                                    //    this is the the most convenient way to execute commands

await $@"docker logs --tail 10 \    //      backslash (\) as line continuation symbol
         {container}";              //        could be used with verbatim strings

await "app 'quoted arg'"            //   use ' quote for arguments that contain spaces
await "app 'quoted '' quote'"       //       '' quote to insert a single quote

await $"app '{path}'"               //   you may quote interpolations that may contain space
await $"app {path}"                 //     but Nake will do it automatically for you ;)
await $"app {arg}\\;;;"             //   and MedallionShell will properly escape the arguments

class Azure                         //  namespace declarations cannot be used with scripts,
{                                   //  but could be easily emulated with class declarations
    class Queue                     //     and you can nest them infinitely as you like
    {    
        [Task] void Clean()         //     then from the command line you would invoke
        {}                          //   this task by its class path (ie, azure queue clean)
    }
}

[Nake] void Default() => Build();   //          running Nake without any options 
                                    //       will cause it to run the "default" task
```

## Command line reference

General syntax is: `Nake [options ...]  [VAR=VALUE ...]  [task ...]`

```cs
> Nake -f "Nake.csx" Log=1 build    //       set Log environment variable to 1 and
                                    //      then run Build() task from Nake.csx file 
                                        
> Nake Log=1 build                  //  equivalent to the above as Nake will automatically try 
                                    //   to use Nake.csx file if present in current directory
```

Options:

	   -?  --help             Display help message and exit
	   -v  --version          Display the program version and exit
	   -q  --quiet            Do not echo informational messages to standard output
	   -s  --silent           Same as --quiet but also suppresses user generated log messages
	   -f  --nakefile FILE    Use FILE as Nake project file
	   -d  --directory DIR    Use DIR as current directory
	   -t  --trace            Enables full stack traces in error reporting + task execution trace
	       --debug            Enables full script debugging in Visual Studio
	   -T  --tasks [PATTERN]  Display tasks with descriptions matching optional PATTERN and exit
	   -r  --reset-cache      Resets compilation output cache

### Invoking tasks

General syntax for invoking tasks and passing arguments is similar to the normal C# method invocation syntax, except ` ` is used instead of `,` to separate task arguments, and `=` is used instead of `:` for specifying named argument values. Also, boolean arguments support special `--` switch syntax.

```cs
> Nake build                          //  run Build task with default arg values
> Nake build Release                  //  or with first positional argument set to 'Release'
> Nake build cfg=Release              //  or with named argument 'cfg' set to 'Release'
> Nake build Release outDir="/c/Tmp"  //  you can mix positional and named arguments
> Nake build ; test                   //  or invoke multiple tasks within a same session
> Nake build `; test                  //  also escape ';' when running in PowerShell console 
> Nake publish --beta                 //  invoke Publish task with 'beta' arg set to 'true'
```

## Included utility reference

Out-of-the box Nake includes a lot of useful convinient utility functions to help you with: 

- running external tools, such as command-line commands or MSBuild
- selecting and transforming file system paths (globber)
- casual file system tasks, such as copying, moving, deleting files/folders 
- logging messages to console
- working with environment variables
- controlling Nake's runner
- etc

Check out table below for reference on using utility library:

| Class         						                    | Functions     					 |
|:----------------------------------------------------------|:------------------------------------------------------|
| [Shell](https://github.com/yevhen/Nake/wiki/Shell)          	| Executing programs and shell commands             |
| [Session](https://github.com/yevhen/Nake/wiki/Session)       	| Controlling current Nake's runner session         |
| [Log](https://github.com/yevhen/Nake/wiki/Log)          	| Logging messages to console                       |
| [Env](https://github.com/yevhen/Nake/wiki/Env)          	| Working with environment variables                |
| [FS](https://github.com/yevhen/Nake/wiki/FS)            	| File-system tasks, such as copy/move/del/mkdir/etc|
| [FileSet](https://github.com/yevhen/Nake/wiki/FileSet)  	| File path selection and transformation (globber)  |
| [Color](https://github.com/yevhen/Nake/wiki/Color)      	| Printing to console in color                      |
| [Location](https://github.com/yevhen/Nake/wiki/Location)	| Current directory and special paths (working, startup) |

Also, see 'by use-case' reference on [wiki](https://github.com/yevhen/Nake/wiki).

## Tips & tricks

```cs
class Azure
{                                       
    StorageAccount account;
    
    static Azure()                  //  this will run once before any of the 
    {                               //  tasks in this namespace are executed
        account = Init();           //  (useful for one-off initialization)
    }
}  
```

## Backlog

- Check the open issues

## Contributing

Make sure you follow the coding guidelines which exists on a project (either as R# formatting settings or .editorconfig). Nothing else)

## Samples and Documentation

Have a look at [Nake.csx](https://github.com/yevhen/Nake/blob/master/Nake.csx). It's a Nake file used to build and publish Nake itself (ye, we're eating our own dog food).

## Community

General discussion group could be found [here](https://gitter.im/yevhen/Nake). Also, for news you can follow Nake's [official](https://twitter.com/NakeTool) twitter account (or [my](https://twitter.com/yevhen) account for that matter). The twitter's hashtag is `#naketool`.

## Credits
- Thanks to everyone in the Roslyn compiler team for making this happen
- Thanks to everyone in the dotnet-script team
- Thanks to MedallionShell community for the great project
- Special thanks to Anton Martynenko for giving me an idea and steering Nake's DSL in the right direction
- Hugs and kisses to my lovely Valery for being so patient and supportive, and for feeding me food and beer, while I was sitting in my cage working on Nake, instead of spending that time with her

## License

Apache 2 License
