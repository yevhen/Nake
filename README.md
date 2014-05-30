## Nake

Nake is a magic task runner tool for .NET. It's a hybrid of Shovel and Rake. The DSL for defining tasks is uniquely minimal and it's just plain C# code! Nake is built on top of the latest Roslyn release so you can use all of the C# V6 features in you scripts and even more.

### How to install

There multiple ways in which Nake could be installed. You can install it by using NuGet [package](https://www.nuget.org/packages/Nake), or you can get it by downloading a [standalone](https://github.com/yevhen/Nake/releases) executable from GitHub releases page, and of course you can always build it from sources. 

To install Nake via NuGet, run this command in NuGet package manager console:

	PM> Install-Package Nake

## Scripting reference

```cs
#r "System"                             // 
#r "System.Core"	                    //   #reference assemblies from the GAC 
#r "System.Data"	                    //    (these are referenced by default)
#r "System.Xml"                         //
#r "System.Xml.Linq"                    //

#r "WindowsBase, Version=4.0.0.0 ..."   //  you can reference assembly by its full name
#r "Packages\NUnit.2.6.2\nunit.dll"     //        or by using relative path
#r "C:\Orleans\SDK\Orleans.dll"         //            or by absolute path

#load "Other.csx"                       //      #load code from other script files
#load "Build\Another.csx"               //  (both absolute and relative paths are fine)

using System;                           //
using System.IO;                        //      standard C# namespace imports
using System.Linq;                      //     (these are imported by default)
using System.Text;                      //  
using System.Collections.Generic;       //  

using System.IO.Path;                   //    C# V6 "using static members" feature 
using System.Console;                   //      will make you scripts more terse

WriteLine("Are you ready? Y/N:");       //      any code you put on the script level 
if (ReadLine() == "N")                  //  will run before any of the tasks are executed
    Exit("See you soon ...");           //      (useful for one-off initialization)

var greeting = "Hello";                 //   you can override any script-level variables 
var who = "world";                      //  with the values passed from the command line

/// Prints greeting                     //  this F#-style summary will be shown in the task listing
[Task] void Welcome()                   //  [Task] makes method runnable from the command line
{                                       
	WriteLine("{greeting},{who}!");     //  forget ugly string.Format and string concatenation 
}                                       //  with built-in support for string interpolation

[Task] void Tell(
    string what = "Hello",              //     for parameterized tasks you can supply
    string whom = "world",              //     arguments directly from the command line
    int times = 1,                      //  (string, int and boolean arguments are supported) 
    bool quiet = false                  //  + special switch syntax for booleans (eg, --quiet)
)
{
    var emphasis = quiet ? "" : "!";
    for (; times > 0; times--)
	    WriteLine("{what},{whom}{emphasis}");
}                                   

[Step] void Clean()   			        //      Steps are Tasks with 'run once' semantics      
{					                    //  (the foundation of any popular build automation tool)
    Delete("{OutputPath}\*.*");	
}                                   

[Step] void Build(string cfg = "Debug")
{					                    
    Clean();                            //  unlike popular build automation tools, there is no any
    -------                             //    special syntax to specify task (step) dependencies
    MSBuild("Nake.sln", cfg);           //      (it's just plain old C# method invocation)
}                                       
                                       
[Step] void Test()
{					                    
    Clean();                            //         you have complete control over decision,
    Build();                            //     when and in what order dependent steps should run
    -------                             //  (Nake will guarantee that any step will run only once)
    NUnit("{OutputPath}\*.Tests.dll")   
}

[Step] void Publish(bool beta = false)
{					                    
    Test();                             //   sometimes, you need to execute the same step but with
    Build("Release");                   //  different arguments. Unlike other build automation tools
    ------                              //  there is no special syntax to force step to run again - 
    Nuget("Nake.nuspec", beta)          //       you just invoke it with different arguments!
}                                       

var apiKey = "$NugetKey$";              //  $var$ is the shortcut syntax for getting 
Push(apiKey, "{PackagePath}");          //      value of environment variable

Write("$NakeStartupDirectory$");        //  these special environment variables
Write("$NakeScriptDirectory$");         //   are automatically created by Nake

Write("{{esc}}");                       //  will simply print {esc} (no string interpolation)
Write("$$esc$$");                       //  will simply print $esc$ (no env variable inlining)

class Azure                             //  namespace declarations cannot be used with scripts,
{                                       //  but could be easily emulated with class declarations
    class Queue                         //     and you can nest them infinitely as you like
    {    
        [Task] void Clean()             //     then from the command line you would invoke
        {}                              //  this task by its full path (ie, azure.queue.clean)
    }
}
```

## Command line reference

General syntax is: `Nake [options ...]  [VAR=VALUE ...]  [task ...]`

Options:

	   -?  --help             Display help message and exit
	   -v  --version          Display the program version and exit
	   -q  --quiet            Do not echo informational messages to standard output
	   -s  --silent           Same as --quiet but also suppresses user generated log messages
	   -f  --nakefile FILE    Use FILE as Nake project file
	   -d  --directory DIR    Use DIR as current directory
	   -t  --trace            Enables task execution tracing and full stack traces in exception messages
	       --debug            Enables full script debugging in Visual Studio
	   -T  --tasks [PATTERN]  Display the tasks with descriptions matching optional PATTERN and exit

## Included utility reference

## Tips & tricks

```cs
class Azure
{                                       
    StorageAccount account;
    
    static Azure()                      //  this will run once before any of the 
    {                                   //  tasks in this namespace are executed
        account = Init();               //  (useful for one-off initialization)
    }
}  
```

## Backlog

- Running on Mono
- Interactive mode

## Contributing

Gimme your pull requests!

## Samples and Documentation

Have a look at [Nake.csx](https://github.com/yevhen/Nake/blob/dev/Nake.csx) or [Publish.csx](https://github.com/yevhen/Nake/blob/dev/Publish.csx). Those are the Nake files used to build and publish Nake itself (ye, we're eating our own dog food). Also, additional samples can be contributed to samples [repository](https://github.com/yevhen/Nake-samples).

## Community

General discussion group could be found [here](https://groups.google.com/forum/#!forum/naketool). Also, for news you can follow Nake's [official](https://twitter.com/NakeTool) twitter account (or [my](https://twitter.com/yevhen) account for that matter). The twitter's hashtag is `#naketool`.

## Credits
- Thanks to everyone in the Roslyn compiler team for making this happen
- Thanks to all members of the [scriptcs](https://github.com/scriptcs) team for lending me their script pre-processing code
- Special thanks to [Anton Martynenko](https://twitter.com/aamartynenko) for giving me an idea and steering Nake's DSL in the right direction
- Hugs and kisses to my lovely Valery for being so patient and supportive, and for feeding me food and beer, while I was sitting in my cage working on Nake, instead of spending that time with her

## License

Apache 2 License
