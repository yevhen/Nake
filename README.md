## Nake

Nake is a magic task runner tool for .NET. It's a hybrid of Shovel and Rake. The DSL for defining tasks is uniquely minimal and it's just plain C# code! Nake is built on top of the latest Roslyn release so you can use all of the C# V6 features in you scripts and even more.

### How to install

There multiple ways in which Nake could be installed. You can install it by using NuGet [package](https://www.nuget.org/packages/Nake), or you can install it by downloading a [standalone](https://github.com/yevhen/Nake/releases) executable from GitHub releases page, and of course you can always build it from sources. 

To install Nake via NuGet, run this command in NuGet package manager console:

	PM> Install-Package Nake

## Scripting example

```cs

#r "System"                             // 
#r "System.Core"	                    //  use r# to reference assemblies from the GAC 
#r "System.Data"	                    //      (these are referenced by default)
#r "System.Xml"                         //
#r "System.Xml.Linq"                    //

// you can also reference assembly by its full name
#r "System.ServiceProcess, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" 
#r "Packages\NUnit.2.6.2\NUnit.dll"     // or by using relative path
#r "C:\Orleans\SDK\Orleans.dll"         // or by absolute path

#load "Other.csx"                       //      load code from other script files
#load "Build\Another.csx"               //  (both absolute and relative paths are fine)

using System;                           //
using System.IO;                        //   standard C# namespace imports
using System.Linq;                      //  (these are imported by default)
using System.Text;                      //  
using System.Collections.Generic;       //  

using System.IO.Path;                   //  C# V6 "using static members" feature 
using System.Console;                   //   will make you scripts more terse

WriteLine("Are you ready? Y/N:");       //      any code you put on the script level 
if (ReadLine() == "N")                  //  will run before any of the tasks are executed
    Exit.Fail("See you soon ...");      //      (useful for one-off initialization)

var greeting = "Hello";                 //   you can override any script-level variables 
var who = "world";                      //  with the values passed from the command line

[Task] void Welcome()                   //  [Task] makes method runnable from the command line
{                                       
	WriteLine("{greeting},{who}!");     //  forget ugly string.Format and string concatenation 
}                                       //  with built-in support for string interpolation

[Task] void Tell(
    string what = "Hello",              //     for parameterized tasks you can supply
    string whom = "world",              //     arguments directly from the command line
    int times = 1,                      //  (string, int and boolean arguments are supported) 
    bool quiet = false                  //  + special switch syntax for booleans (ie, --quiet)
)
{
    var emphasis = quiet ? "" : "!";
    for (; times > 0; times--)
	    WriteLine("{what},{whom}{emphasis}");
}                                   

```

## Command line example

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

>NOTE: You can always get help for command line usage by running Nake with *-?* or *--help* switches.

## Included utility example



## Backlog

- Running on Mono
- Interactive mode

## Contributing

Gimme your pull requests!

## Samples and Documentation

Have a look at [Nake.csx](https://github.com/yevhen/Nake/blob/dev/Nake.csx) or [Publish.csx](https://github.com/yevhen/Nake/blob/dev/Publish.csx). Those are the Nake files used to build and publish Nake itself (ye, we're eating our own dog food). Also, additional samples can be contributed to samples [repository](https://github.com/yevhen/Nake-samples). Detailed documentation (soon) can be found on [wiki](https://github.com/yevhen/Nake/wiki). 

## Community

General discussion group could be found [here](https://groups.google.com/forum/#!forum/naketool). Also, for news you can follow Nake's [official](https://twitter.com/NakeTool) twitter account (or [my](https://twitter.com/yevhen) account for that matter). The twitter's hashtag is `#naketool`.

## Credits
- Thanks to everyone in the Roslyn compiler team for making this happen
- Thanks to all members of the [scriptcs](https://github.com/scriptcs) team for lending me their script pre-processing code
- Special thanks to [Anton Martynenko](https://twitter.com/aamartynenko) for giving me an idea and steering Nake's DSL in the right direction
- Hugs and kisses to my lovely Valery for being so patient and supportive, and for feeding me food and beer, while I was sitting in my cage working on Nake, instead of spending that time with her

## License

Apache 2 License
